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
using WorkflowHostWinService;
using Host = Microsoft.Extensions.Hosting.Host;


// Configure a logger
ILogger logger = new WindowsEventLogger();

try
{
    // Get configuration values
    IConfiguration configuration = new ConfigurationBuilder().SetBasePath(GetBasePath()).AddJsonFile("appsettings.json", false, true).Build();
    var workflowHostOptions = configuration.GetRequiredSection("WorkflowHostOptions").Get<WorkflowHostOptions>();

    // Configure a scalar service (optional)
    var disableScalarService = configuration.GetValue("ScalarService:Disable", false);
    var disableScalarServiceLogging = configuration.GetValue("ScalarService:DisableLogging", false);
    GroupedScalarService? scalarService = null;
    if (!disableScalarService)
    {
        var scalarRepository = new ScalarRepository("scalars.json");
        scalarService = disableScalarServiceLogging ? new GroupedScalarService(scalarRepository) : new GroupedScalarService(scalarRepository, logger);
    }

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