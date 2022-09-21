using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;
using DHI.Services.Logging;
using DHI.Services.Provider.Windows;
using DHI.Services.Scalars;
using DHI.Workflow.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkflowHostWinService;
using Host = Microsoft.Extensions.Hosting.Host;
using Timer = System.Timers.Timer;

var workflowHostOptions = new WorkflowHostOptions
{
    // The port that the host is listening for requests on
    Port = 7777, 
    // If a folder is present matching the pattern of UpdatesPath, the Workflow Host will go offline and wait until all workflows have finished. After that the files in the updates folder are moved to the location where the workflow are executed. After that the Workflow Host resumes operation
    UpdateEnabled = true,
    UpdateTimerIntervalInMinutes = 1,
    UpdatesPath = "Updates*",
    // Environment variable is extracted and used to attribute scalars
    MachineIdEnvironmentVariable = "COMPUTERNAME",
    // The path where the workflow execution is performed. An empty string indicates that the execution is done next to the Workflow Host. Engine path can be used to segregate the execution an updating from the host itself
    EnginePath = string.Empty
};

#warning Select an appropriate logger. By default a Windows Event logger is configured. In production systems, a PostgreSQL based log repository or similar should be used
//ILogger logger = new WindowsEventLogger();
using var processModule = Process.GetCurrentProcess().MainModule;
ILogger logger = new SimpleLogger(Path.Combine(Path.GetDirectoryName(processModule?.FileName), "WorkflowHostWinService.log"));

Timer? windowsUpdateTimerAutoRestart = null;
GroupedScalarService? scalarService = null;

try
{
    // Configure a scalar service (optional)
#warning Comment in if the scalar service should be used. The Scalar service enables updating of scalars such as the number of workflows running on the host etc. The scalar respository should in production systems be changed to e.g. the PostgreSQL based scalar repository
    // var scalarRepository = new ScalarRepository("scalars.json");

#warning Comment in to use the scalar service without logging
    // scalarService = new GroupedScalarService(scalarRepository, logger);

#warning Comment in to use the scalar service without logging
    // scalarService = new GroupedScalarService(scalarRepository)

    // Setup windows update timer (optional)
#warning Comment in to allow for functionality for restarting Windows servers automatically after application of patches. This allows for configuration of servers to download and apply patches. If a restart is required, the workflow host will set is to be unavailable until any workflows currently being executed are completed and then restart the server
    //var windowsUpdateAutoRestartTimerIntervalInMinutes = 1; // The frequency with which it is checked if there is a restart pending
    //windowsUpdateTimerAutoRestart = new Timer { Interval = windowsUpdateAutoRestartTimerIntervalInMinutes * 1000 * 60 };
    //windowsUpdateTimerAutoRestart.Elapsed += WindowsUpdateTimerElapsed;

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
#warning Comment in to allow for functionality for restarting Windows servers automatically after application of patches.
            //services.AddScoped(_ => windowsUpdateTimerAutoRestart!);
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
    logger.Log(new LogEntry(logLevel, message, "Workflow Host"));
}

void WindowsUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
{
    try
    {
        windowsUpdateTimerAutoRestart.Stop();
        bool reboot;

        // For testing purpose
        if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimulateWindowsReboot.txt")))
        {
            reboot = WindowsUpdateInformation.RebootRequired();
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
        windowsUpdateTimerAutoRestart.Start();
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