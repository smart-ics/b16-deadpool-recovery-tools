using Deadpool.Core.Domain.Enums;
using Deadpool.Core.Interfaces;
using Deadpool.Core.Workflow;
using Quartz;
using System.Diagnostics;

namespace Deadpool.Cli.Jobs;

/// <summary>
/// Quartz job that executes a single backup operation via BackupWorkflow.
///
/// Job data map must include:
///   - DatabaseId (Guid)
///   - BackupType (BackupType enum)
///
/// DisallowConcurrentExecution prevents the same job instance (database + type)
/// from running concurrently. Combined with BackupWorkflow's GetActiveJobAsync guard,
/// this provides dual-layer overlap protection.
/// </summary>
[DisallowConcurrentExecution]
public sealed class BackupJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackupJob> _logger;

    public BackupJob(IServiceScopeFactory scopeFactory, ILogger<BackupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var jobData = context.JobDetail.JobDataMap;

        // Extract job parameters from data map
        var databaseId = jobData.GetGuid("DatabaseId");
        var backupType = (BackupType)jobData.GetInt("BackupType");

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Backup job starting: {DatabaseId} | {BackupType} | JobId={JobKey}",
            databaseId, backupType, context.JobDetail.Key);

        try
        {
            // Create scope to resolve scoped services (BackupWorkflow, repositories)
            await using var scope = _scopeFactory.CreateAsyncScope();

            var workflow = scope.ServiceProvider.GetRequiredService<BackupWorkflow>();
            var profileRepo = scope.ServiceProvider.GetRequiredService<IDatabaseProfileRepository>();

            // Load profile
            var profile = await profileRepo.GetByIdAsync(databaseId, context.CancellationToken);
            if (profile == null)
            {
                _logger.LogError(
                    "DatabaseProfile not found: {DatabaseId}. Job will be skipped.",
                    databaseId);
                return;
            }

            // Execute backup workflow
            await workflow.RunAsync(profile, backupType, context.CancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Backup job completed: {DatabaseId} | {DatabaseName} | {BackupType} | Duration={Duration}ms",
                databaseId, profile.DatabaseName, backupType, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Backup job failed: {DatabaseId} | {BackupType} | Duration={Duration}ms",
                databaseId, backupType, stopwatch.ElapsedMilliseconds);

            // Re-throw so Quartz marks the execution as failed
            throw new JobExecutionException(
                $"Backup job failed for database {databaseId}, type {backupType}", ex, refireImmediately: false);
        }
    }
}

