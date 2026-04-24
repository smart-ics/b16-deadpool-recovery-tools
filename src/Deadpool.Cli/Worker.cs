namespace Deadpool.Cli;

/// <summary>
/// Hosted service lifecycle wrapper.
/// Actual backup scheduling is handled by Quartz jobs.
/// This worker is responsible for startup/shutdown logging and any
/// non-scheduled housekeeping tasks (e.g. heartbeat, health probe).
/// </summary>
public sealed class DeadpoolWorker : BackgroundService
{
    private readonly ILogger<DeadpoolWorker> _logger;

    public DeadpoolWorker(ILogger<DeadpoolWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Deadpool worker is running. Backup scheduling is managed by Quartz.");

        // Keep the hosted service alive; Quartz drives all backup triggers.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deadpool Backup Tools started.");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deadpool Backup Tools stopping.");
        return base.StopAsync(cancellationToken);
    }
}
