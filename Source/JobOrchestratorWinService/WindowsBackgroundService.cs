namespace JobOrchestratorWinService
{
    using DHI.Services.Jobs.Orchestrator;
    using DHI.Services.Logging;
    using Microsoft.Extensions.Hosting;
    using System.Threading;
    using System.Threading.Tasks;

    public class WindowsBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly JobOrchestrator _jobOrchestrator;

        public WindowsBackgroundService(JobOrchestrator jobOrchestrator, ILogger logger)
        {
            _jobOrchestrator = jobOrchestrator;
            _logger = logger;
            var (cleaningLogLevel, cleaningStatus) = _jobOrchestrator.CleaningEnabled() ? (LogLevel.Information, "enabled") : (LogLevel.Warning, "disabled");
            _logger.Log(new LogEntry(cleaningLogLevel, $"Cleaning is {cleaningStatus}.", ServiceName));
            var scalarsStatus = _jobOrchestrator.ScalarsEnabled() ? "enabled" : "disabled";
            _logger.Log(new LogEntry(cleaningLogLevel, $"Scalars are {scalarsStatus}.", ServiceName));
        }

        public static string ServiceName => "DHI Job Orchestrator";

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _jobOrchestrator.Start();
            _logger.Log(new LogEntry(LogLevel.Information, "Background service started.", ServiceName));
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _jobOrchestrator.Stop();
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
