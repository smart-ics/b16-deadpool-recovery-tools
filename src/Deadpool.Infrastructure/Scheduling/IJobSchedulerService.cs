using Quartz;

namespace Deadpool.Infrastructure.Scheduling;

/// <summary>
/// Service responsible for dynamically registering Quartz jobs and triggers
/// based on DatabaseProfile records in the catalog.
/// </summary>
public interface IJobSchedulerService
{
    /// <summary>
    /// Loads all active DatabaseProfile records and registers corresponding
    /// Quartz jobs + triggers with the scheduler.
    /// Called once at application startup.
    /// </summary>
    Task ScheduleJobsAsync(IScheduler scheduler, CancellationToken cancellationToken = default);
}

