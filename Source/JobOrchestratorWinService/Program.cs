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
using DHI.Services.Provider.DS;
using CodeWorkflowRepository = DHI.Services.Provider.DS.CodeWorkflowRepository;
using Host = Microsoft.Extensions.Hosting.Host;
using HostRepository = DHI.Services.Provider.DS.HostRepository;
using JobRepository = DHI.Services.Provider.DS.JobRepository;
using JobService = DHI.Services.Jobs.JobService;

#warning Select an appropriate logger. By default a Windows Event logger is configured. In production systems, a PostgreSQL based log repository or similar should be used
//ILogger logger = new WindowsEventLogger();
using var processModule = Process.GetCurrentProcess().MainModule;
ILogger logger = new SimpleLogger(Path.Combine(Path.GetDirectoryName(processModule?.FileName), "JobOrchestratorWinService.log"));

try
{
    // Get configuration values
    IConfiguration configuration = new ConfigurationBuilder().SetBasePath(GetBasePath()).AddJsonFile("appsettings.json", false, true).Build();

    // Configure a scalar service (optional)
    GroupedScalarService? scalarService = null;

#warning Comment in if the scalar service should be used. The Scalar service enables updating of scalars such as the number of workflows running on the host etc. The scalar respository should in production systems be changed to e.g. the PostgreSQL based scalar repository
    // var scalarRepository = new ScalarRepository("scalars.json");

#warning Comment in to use the scalar service without logging
    // scalarService = new GroupedScalarService(scalarRepository, logger);

#warning Comment in to use the scalar service without logging
    // scalarService = new GroupedScalarService(scalarRepository)

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

#warning Set the frequency with which the job repositories are queried for new jobs.
    const int executionTimerIntervalInMilliseconds = 10 * 1000;

#warning Set the frequency with which jobs that have been running for too long are checked
    const int cleaningTimerIntervalInMilliseconds = 3600 * 1000;

    // Create job orchestrator
    JobOrchestrator jobOrchestrator;
    if (scalarService is null)
    {
        jobOrchestrator = new JobOrchestrator(jobWorkers, logger, executionTimerIntervalInMilliseconds, cleaningTimerIntervalInMilliseconds);
    }
    else
    {
        jobOrchestrator = new JobOrchestrator(jobWorkers, logger, executionTimerIntervalInMilliseconds, scalarService, jobServices, cleaningTimerIntervalInMilliseconds);
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

#warning Set the default maximum duration of a job. May be overridden by task-specific maximum durations. 
    var jobTimeout = TimeSpan.FromDays(1);

#warning Set the period after which jobs not started are set to Error
    var startTimeout = TimeSpan.FromMinutes(2);

#warning Set the period after which jobs running are set to Error
    var maxAge = TimeSpan.MaxValue;

    // Create job worker for code workflows
    const string userName = "frt";
    const string password = "DS_Course22";
    const string authServerUrl = "http://localhost:5001";
    const string apiServerUrl = "http://localhost:5000";

    var tokenProvider = new AccessTokenProvider($"baseUrl={authServerUrl};userName={userName};password={password}", serviceLogger);

    // Tasks
    var taskRepository = new CodeWorkflowRepository($"{apiServerUrl}/api/tasks/wf-tasks", tokenProvider, 3, serviceLogger);
    var taskService = new CodeWorkflowService(taskRepository);
    
    // Jobs
    var jobRepository = new JobRepository($"{apiServerUrl}/api/jobs/wf-jobs", tokenProvider, 3, serviceLogger);
    var jobService = new JobService(jobRepository, taskService);
    
    // Hosts
    var hostRepository = new HostRepository($"{apiServerUrl}/api/jobhosts", tokenProvider, 3, serviceLogger);
    var hostService = new HostService(hostRepository);

    // Logs
    using var processModule = Process.GetCurrentProcess().MainModule;
    var workerLogger = new WorkflowLogger(Path.Combine(Path.GetDirectoryName(processModule?.FileName), "Log"));
    var worker = new CodeWorkflowWorker(workerLogger);

    const string jobWorkerId = "MyJobWorker";
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