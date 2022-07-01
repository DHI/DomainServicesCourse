namespace WorkflowHostWinService
{
    using DHI.Services.Logging;
    using DHI.Workflow.Host;
    using Microsoft.Extensions.Hosting;
    using System.Threading;
    using System.Threading.Tasks;

    public class WindowsBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly WorkflowHost _workflowHost;

        public WindowsBackgroundService(WorkflowHost workflowHost, ILogger logger)
        {
            _workflowHost = workflowHost;
            _logger = logger;
            var scalarsStatus = _workflowHost.ScalarsEnabled ? "enabled" : "disabled";
            _logger.Log(new LogEntry(LogLevel.Information, $"Scalars are {scalarsStatus}.", ServiceName));
        }

        public static string ServiceName => "DHI Workflow Host";

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _workflowHost.Start();
            _logger.Log(new LogEntry(LogLevel.Information, "Background service started.", ServiceName));
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _workflowHost.Stop();
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
}
