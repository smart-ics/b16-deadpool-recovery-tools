using Deadpool.Infrastructure.Scheduling;
using Quartz;

namespace Deadpool.Cli.Jobs;

/// <summary>
/// Hosted service that runs once at startup to load DatabaseProfile records
/// and register corresponding Quartz jobs/triggers.
/// </summary>
public sealed class SchedulerBootstrapService : IHostedService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobSchedulerService _jobSchedulerService;
    private readonly ILogger<SchedulerBootstrapService> _logger;

    public SchedulerBootstrapService(
        ISchedulerFactory schedulerFactory,
        IJobSchedulerService jobSchedulerService,
        ILogger<SchedulerBootstrapService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _jobSchedulerService = jobSchedulerService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SchedulerBootstrapService: Initializing backup job schedules...");

        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await _jobSchedulerService.ScheduleJobsAsync(scheduler, cancellationToken);

            _logger.LogInformation("SchedulerBootstrapService: Backup schedules initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SchedulerBootstrapService: Failed to initialize backup schedules.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Quartz shutdown is handled by QuartzHostedService
        _logger.LogInformation("SchedulerBootstrapService: Shutdown complete.");
        return Task.CompletedTask;
    }
}

