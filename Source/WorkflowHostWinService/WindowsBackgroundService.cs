namespace WorkflowHostWinService;

using System.Threading;
using System.Threading.Tasks;
using DHI.Services.Logging;
using DHI.Workflow.Host;
using Microsoft.Extensions.Hosting;
using Timer = System.Timers.Timer;

public class WindowsBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly WorkflowHost _workflowHost;
    private readonly Timer? _windowsUpdateTimer;

    public WindowsBackgroundService(WorkflowHost workflowHost, ILogger logger, Timer? windowsUpdateTimer = null)
    {
        _workflowHost = workflowHost;
        _logger = logger;
        _windowsUpdateTimer = windowsUpdateTimer;
        var scalarsStatus = _workflowHost.ScalarsEnabled ? "enabled" : "disabled";
        _logger.Log(new LogEntry(LogLevel.Information, $"Scalars are {scalarsStatus}.", ServiceName));
    }

    public static string ServiceName => "DHI Workflow Host";

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _workflowHost.Start();
        _logger.Log(new LogEntry(LogLevel.Information, "Background service started.", ServiceName));
        if (_windowsUpdateTimer is not null)
        {
            _logger.Log(new LogEntry(LogLevel.Information, "Windows Update enabled.", ServiceName));
            _windowsUpdateTimer.Start();
        }
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _workflowHost.Stop();
        _windowsUpdateTimer?.Stop();
        _logger.Log(new LogEntry(LogLevel.Information, "Background service stopped.", ServiceName));
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}