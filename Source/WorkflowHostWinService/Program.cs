using DHI.Services.Logging;
using DHI.Services.Provider.Windows;
using DHI.Services.Scalars;
using DHI.Workflow.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;
using WorkflowHostWinService;
using WUApiLib;
using Host = Microsoft.Extensions.Hosting.Host;
using Timer = System.Timers.Timer;


// Configure a logger
ILogger logger = new WindowsEventLogger();

WorkflowHostOptions workflowHostOptions;
Timer windowsUpdateTimer;
GroupedScalarService? scalarService = null;


try
{
    // Get configuration values
    IConfiguration configuration = new ConfigurationBuilder().SetBasePath(GetBasePath()).AddJsonFile("appsettings.json", false, true).Build();
    workflowHostOptions = configuration.GetRequiredSection("WorkflowHostOptions").Get<WorkflowHostOptions>();

    // Configure a scalar service (optional)
    var disableScalarService = configuration.GetValue("ScalarService:Disable", false);
    var disableScalarServiceLogging = configuration.GetValue("ScalarService:DisableLogging", false);
    if (!disableScalarService)
    {
        var scalarRepository = new ScalarRepository("scalars.json");
        scalarService = disableScalarServiceLogging ? new GroupedScalarService(scalarRepository) : new GroupedScalarService(scalarRepository, logger);
    }

    // Setup windows update timer
    var disableWindowsUpdate = !configuration.GetValue("WindowsUpdate:Disable", false);
    windowsUpdateTimer = new Timer { Interval = configuration.GetValue<int>("WindowsUpdate:TimerIntervalInMinutes") * 1000 * 60 };
    windowsUpdateTimer.Elapsed += WindowsUpdateTimerElapsed;

    // Create workflow host
    var workflowHost = new WorkflowHost(logger, scalarService, workflowHostOptions);

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
            services.AddScoped(_ => workflowHost);
            if (!disableWindowsUpdate)
            {
                services.AddScoped(_ => windowsUpdateTimer);
            }
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

void WriteLog(string message, LogLevel logLevel = LogLevel.Information)
{
    logger.Log(new LogEntry(logLevel, message, WindowsBackgroundService.ServiceName));
}

string? GetBasePath()
{
    using var processModule = Process.GetCurrentProcess().MainModule;
    return Path.GetDirectoryName(processModule?.FileName);
}

void WindowsUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
{
    try
    {
        windowsUpdateTimer.Stop();
        bool reboot;
        if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimulateWindowsReboot.txt")))
        {
            ISystemInformation systemInfo = new SystemInformation();
            reboot = systemInfo.RebootRequired;
        }
        else
        {
            WriteLog("(Detected SimulateWindowsReboot)");
            reboot = true;
        }

        CodeWorkflowHostController.AwaitWindowsUpdate = reboot;
        if (reboot)
        {
            WriteLog("Reboot is required, waiting for workflows to finish");
            var scalarGroup = $"Workflow/{Environment.GetEnvironmentVariable(workflowHostOptions.MachineIdEnvironmentVariable) ?? $"{workflowHostOptions.MachineIdEnvironmentVariable} does not exist"}";
            if (scalarService is not null)
            {
                const string scalarNameUpdatePending = "Restart Pending";
                scalarService.TrySetDataOrAdd(new Scalar<string, int>($"{scalarGroup}/{scalarNameUpdatePending}", scalarNameUpdatePending, "System.Boolean", scalarGroup, new ScalarData<int>(true, DateTime.UtcNow)));
            }

            WaitForWorkflowsToFinish();
            if (scalarService is not null)
            {
                const string scalarNameUpdatePending = "Restart Pending";
                scalarService.TrySetDataOrAdd(new Scalar<string, int>($"{scalarGroup}/{scalarNameUpdatePending}", scalarNameUpdatePending, "System.Boolean", scalarGroup, new ScalarData<int>(false, DateTime.UtcNow)));
            }

            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimulateWindowsReboot.txt")))
            {
                WriteLog("Rebooting");
                Process.Start("shutdown", "/r /t 0");
            }
            else
            {
                WriteLog("Here it would have rebooted");
            }
        }
    }
    catch (Exception ex)
    {
        WriteLog(ex.Message, LogLevel.Error);
    }
    finally
    {
        windowsUpdateTimer.Start();
    }
}

void WaitForWorkflowsToFinish()
{
    bool NeedToWait()
    {
        var needToWait = false;

        var processCount = Process.GetProcessesByName(WorkflowHost.EngineFullName).Length;
        if (processCount > 0)
        {
            WriteLog($"WindowsUpdating: Waiting for current job execution processes to finish. Currently {processCount} processes.");
            needToWait = true;
        }

        if (CodeWorkflowHostController.AwaitWorkflowUpdate)
        {
            WriteLog("WindowsUpdating: Waiting for workflow update to finish");
            needToWait = true;
        }

        return needToWait;
    }

    while (NeedToWait())
    {
        // Wait for one minute to check if engines are done
        Thread.Sleep(1000 * 60 * 1);
    }
}