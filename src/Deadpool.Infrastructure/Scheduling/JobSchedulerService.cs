using Deadpool.Core.Domain.Enums;
using Deadpool.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Deadpool.Infrastructure.Scheduling;

/// <summary>
/// Loads DatabaseProfile records from the catalog and creates Quartz jobs/triggers.
/// Each database gets up to 3 jobs: Full, Differential, TransactionLog.
/// Misfire policy: FireAndProceed for Full/Diff, DoNothing for Log.
/// </summary>
internal sealed class JobSchedulerService : IJobSchedulerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobSchedulerService> _logger;

    // Quartz job type — resolved dynamically at runtime from Deadpool.Cli assembly
    private static readonly Type BackupJobType = Type.GetType("Deadpool.Cli.Jobs.BackupJob, Deadpool.Cli")
        ?? throw new InvalidOperationException("BackupJob type not found in Deadpool.Cli assembly.");

    public JobSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<JobSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ScheduleJobsAsync(IScheduler scheduler, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading active database profiles for scheduling...");

        // Create scope to resolve scoped IDatabaseProfileRepository
        await using var scope = _scopeFactory.CreateAsyncScope();
        var profileRepository = scope.ServiceProvider.GetRequiredService<IDatabaseProfileRepository>();

        var profiles = await profileRepository.GetAllActiveAsync(cancellationToken);
        var profileList = profiles.ToList();

        if (!profileList.Any())
        {
            _logger.LogWarning("No active database profiles found. No backup jobs will be scheduled.");
            return;
        }

        var jobCount = 0;

        foreach (var profile in profileList)
        {
            _logger.LogInformation(
                "Scheduling jobs for database: {DatabaseName} (ID={DatabaseId})",
                profile.DatabaseName, profile.DatabaseId);

            // ── Full backup ───────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(profile.FullBackupSchedule))
            {
                await ScheduleBackupJobAsync(
                    scheduler,
                    profile.DatabaseId,
                    profile.DatabaseName,
                    BackupType.Full,
                    profile.FullBackupSchedule,
                    useCron: true,
                    cancellationToken);
                jobCount++;
            }
            else
            {
                _logger.LogWarning(
                    "Full backup schedule is empty for {DatabaseName}. Skipping Full job.",
                    profile.DatabaseName);
            }

            // ── Differential backup ───────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(profile.DiffBackupSchedule))
            {
                await ScheduleBackupJobAsync(
                    scheduler,
                    profile.DatabaseId,
                    profile.DatabaseName,
                    BackupType.Differential,
                    profile.DiffBackupSchedule,
                    useCron: true,
                    cancellationToken);
                jobCount++;
            }

            // ── Transaction log backup ────────────────────────────────────────
            if (profile.LogBackupEnabled && profile.LogBackupEveryMinute > 0)
            {
                await ScheduleLogBackupJobAsync(
                    scheduler,
                    profile.DatabaseId,
                    profile.DatabaseName,
                    profile.LogBackupEveryMinute,
                    cancellationToken);
                jobCount++;
            }
        }

        _logger.LogInformation(
            "Scheduling complete: {JobCount} jobs registered for {ProfileCount} databases.",
            jobCount, profileList.Count);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task ScheduleBackupJobAsync(
        IScheduler scheduler,
        Guid databaseId,
        string databaseName,
        BackupType backupType,
        string cronExpression,
        bool useCron,
        CancellationToken cancellationToken)
    {
        var jobKey = new JobKey($"backup-{databaseId}-{backupType.ToString().ToLowerInvariant()}");

        var job = JobBuilder.Create(BackupJobType)
            .WithIdentity(jobKey)
            .WithDescription($"{backupType} backup for {databaseName}")
            .UsingJobData("DatabaseId", databaseId.ToString())
            .UsingJobData("BackupType", (int)backupType)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trigger-{jobKey.Name}")
            .ForJob(jobKey)
            .WithCronSchedule(
                cronExpression,
                x => x.WithMisfireHandlingInstructionFireAndProceed()) // Fire missed jobs ASAP
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);

        _logger.LogInformation(
            "Scheduled: {BackupType} for {DatabaseName} | Cron={Cron}",
            backupType, databaseName, cronExpression);
    }

    private async Task ScheduleLogBackupJobAsync(
        IScheduler scheduler,
        Guid databaseId,
        string databaseName,
        int intervalMinutes,
        CancellationToken cancellationToken)
    {
        var jobKey = new JobKey($"backup-{databaseId}-log");

        var job = JobBuilder.Create(BackupJobType)
            .WithIdentity(jobKey)
            .WithDescription($"Transaction log backup for {databaseName}")
            .UsingJobData("DatabaseId", databaseId.ToString())
            .UsingJobData("BackupType", (int)BackupType.TransactionLog)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trigger-{jobKey.Name}")
            .ForJob(jobKey)
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(intervalMinutes)
                .RepeatForever()
                .WithMisfireHandlingInstructionNextWithRemainingCount()) // DoNothing equivalent
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);

        _logger.LogInformation(
            "Scheduled: Log backup for {DatabaseName} | Interval={Minutes} min",
            databaseName, intervalMinutes);
    }
}

