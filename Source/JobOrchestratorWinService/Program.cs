using DHI.Services;
using DHI.Services.Jobs;
using DHI.Services.Jobs.Orchestrator;
using DHI.Services.Jobs.Workflows;
using DHI.Services.Jobs.WorkflowWorker;
using DHI.Services.Logging;
using DHI.Services.Provider.Windows;
using DHI.Services.Scalars;
using JobOrchestratorWinService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CodeWorkflowRepository = DHI.Services.Provider.DS.CodeWorkflowRepository;
using Host = Microsoft.Extensions.Hosting.Host;
using HostRepository = DHI.Services.Provider.DS.HostRepository;
using JobRepository = DHI.Services.Provider.DS.JobRepository;

// Configure a logger
ILogger logger = new WindowsEventLogger();

try
{
    // Get configuration values
    IConfiguration configuration = new ConfigurationBuilder().SetBasePath(GetBasePath()).AddJsonFile("appsettings.json", false, true).Build();

    // Configure a scalar service (optional)
    var disableScalarService = configuration.GetValue("ScalarService:Disable", false);
    var disableScalarServiceLogging = configuration.GetValue("ScalarService:DisableLogging", false);
    GroupedScalarService? scalarService = null;
    if (!disableScalarService)
    {
        var scalarRepository = new ScalarRepository("scalars.json");
        scalarService = disableScalarServiceLogging ? new GroupedScalarService(scalarRepository) : new GroupedScalarService(scalarRepository, logger);
    }

    // Create job workers
    var (jobWorkers, jobServices) = CreateJobWorkers(logger, configuration);

    // Prepare job workers
    foreach (var jobWorker in jobWorkers)
    {
        jobWorker.Executing += JobWorkerExecuting;
        jobWorker.Executed += JobWorkerExecuted;
        jobWorker.Cancelling += JobWorkerCancelling;
        jobWorker.Cancelled += JobWorkerCancelled;
        jobWorker.Interrupted += JobWorkerInterrupted;
        await Task.Run(() => jobWorker.Clean());
    }

    // Create job orchestrator
    var executionTimerInterval = configuration.GetValue<double>("ExecutionTimerIntervalInSeconds", 10) * 1000;
    var cleaningTimerInterval = configuration.GetValue<double>("CleaningTimerIntervalInMinutes", 60) * 60 * 1000;
    JobOrchestrator jobOrchestrator;
    if (scalarService is null)
    {
        jobOrchestrator = new JobOrchestrator(jobWorkers, logger, executionTimerInterval, cleaningTimerInterval);
    }
    else
    {
        jobOrchestrator = new JobOrchestrator(jobWorkers, logger, executionTimerInterval, scalarService, jobServices, cleaningTimerInterval);
    }

    // Create the Windows service host
    using IHost host = Host.CreateDefaultBuilder(args)
        .UseWindowsService(options =>
        {
            options.ServiceName = WindowsBackgroundService.ServiceName;
        })
        .ConfigureServices(services =>
        {
            services.AddHostedService<WindowsBackgroundService>();
            services.AddScoped(_ => logger);
            services.AddScoped(_ => jobOrchestrator);
        })
        .Build();

    await host.RunAsync();
}
catch (Exception e)
{
    var message = $"{e.Message}. {e.InnerException?.Message}";
    WriteLog(message, LogLevel.Error);
    throw;
}

(IJobWorker<string>[] jobWorkers, Dictionary<string, IJobService<string>> jobServices) CreateJobWorkers(ILogger serviceLogger, IConfiguration configuration)
{
    var jobWorkers = new List<IJobWorker<string>>();
    var jobServices = new Dictionary<string, IJobService<string>>();

    // Get configuration values
    var verboseLogging = configuration.GetValue("VerboseLogging", false); // Set to true to enable verbose logging from within the load balancer.
    if (verboseLogging)
        WriteLog("Verbose logging is enabled.");
    var jobTimeout = configuration.GetValue("JobTimeout", TimeSpan.FromDays(1)); // The default maximum duration of a job. May be overridden by task-specific maximum durations. 
    var startTimeout = configuration.GetValue("StartTimeout", TimeSpan.FromMinutes(2)); // Jobs not started within this period will have their status set to Error.
    var maxAge = configuration.GetValue("MaxAge", TimeSpan.MaxValue);  // Job records older than this timespan will be removed.

    // Create job worker for code workflows
    const string jobWorkerId = "MyJobWorker";
#warning use environment variable for password
    const string baseUrlTokens = "http://localhost:5001;userName=admin;password=webapi";
    var taskService = new CodeWorkflowService(new CodeWorkflowRepository($"baseUrl=http://localhost:5000/api/tasks/wf-tasks;baseUrlTokens={baseUrlTokens}", serviceLogger));
    var jobService = new JobService(new JobRepository($"baseUrl=http://localhost:5000/api/jobs/wf-jobs;baseUrlTokens={baseUrlTokens}", serviceLogger), taskService);
    var hostService = new HostService(new HostRepository($"baseUrl=http://localhost:5000/api/jobhosts;baseUrlTokens={baseUrlTokens}", serviceLogger));
    var workerLogger = new WorkflowLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log"));
    var worker = new CodeWorkflowWorker(workerLogger);
    var loadBalancer = new LoadBalancer(jobWorkerId, worker, jobService, hostService, verboseLogging ? serviceLogger : null);
    var jobWorker = new JobWorker(jobWorkerId, worker, taskService, jobService, hostService, loadBalancer, jobTimeout, startTimeout, maxAge, serviceLogger);
    jobWorkers.Add(jobWorker);
    jobServices.Add(jobWorkerId, jobService);

    return (jobWorkers.ToArray(), jobServices);
}

void JobWorkerExecuted(object? sender, EventArgs<Tuple<Guid, JobStatus, string, string>> e)
{
    var logLevel = e.Item.Item2 == JobStatus.Error ? LogLevel.Error : LogLevel.Information;
    WriteLog($"Job worker '{((IJobWorker<string>)sender!).Id}' executed task '{e.Item.Item3}' with job ID '{e.Item.Item1}'. Status '{e.Item.Item2}'." + (e.Item.Item2 == JobStatus.Error ? $" Message: {e.Item.Item4}" : string.Empty), logLevel);
}

void JobWorkerExecuting(object? sender, EventArgs<Job<Guid, string>> e)
{
    WriteLog($"Job worker '{((IJobWorker<string>)sender!).Id}' executing task '{e.Item.TaskId}' on host '{e.Item.HostId}' with job ID '{e.Item.Id} for account '{e.Item.AccountId}'...");
}

void JobWorkerCancelled(object? sender, EventArgs<Tuple<Guid, string?>> e)
{
    WriteLog($"Job worker '{((IJobWorker<string>)sender!).Id}' cancelled job '{e.Item.Item1}'." + (e.Item.Item2 != null ? $" Message: {e.Item.Item2}" : string.Empty));
}

void JobWorkerCancelling(object? sender, EventArgs<Job<Guid, string>> e)
{
    WriteLog($"Job worker '{((IJobWorker<string>)sender!).Id}' cancelling job '{e.Item.Id} on host '{e.Item.HostId}'...");
}

void JobWorkerInterrupted(object? sender, EventArgs<Guid> e)
{
    WriteLog($"Job worker '{((IJobWorker<string>)sender!).Id}' interrupted while executing job ID '{e.Item}'", LogLevel.Warning);
}

void WriteLog(string message, LogLevel logLevel = LogLevel.Information)
{
    logger.Log(new LogEntry(logLevel, message, WindowsBackgroundService.ServiceName));
}

string? GetBasePath()
{
    using var processModule = Process.GetCurrentProcess().MainModule;
    return Path.GetDirectoryName(processModule?.FileName);
}