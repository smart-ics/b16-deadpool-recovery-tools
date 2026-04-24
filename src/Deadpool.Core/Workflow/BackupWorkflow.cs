using Deadpool.Core.Configuration;
using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;
using Deadpool.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Deadpool.Core.Workflow;

/// <summary>
/// Orchestrates the full backup workflow state machine.
///
/// State transitions (happy path):
///   Pending → Running → BackupCompleted → Copying → Verified → Success
///
/// Copy failure path (max 3 attempts):
///   Copying → RetryPending → Copying → ... → Failed
///
/// Precheck / backup failure (no retry):
///   Pending → [Running] → Failed
/// </summary>
public sealed class BackupWorkflow
{
    private readonly IPrecheckService _precheckService;
    private readonly IBackupExecutor _backupExecutor;
    private readonly IFileCopyService _fileCopyService;
    private readonly IBackupJobRepository _jobRepository;
    private readonly IBackupCatalogRepository _catalogRepository;
    private readonly DeadpoolSettings _settings;
    private readonly ILogger<BackupWorkflow> _logger;

    public BackupWorkflow(
        IPrecheckService precheckService,
        IBackupExecutor backupExecutor,
        IFileCopyService fileCopyService,
        IBackupJobRepository jobRepository,
        IBackupCatalogRepository catalogRepository,
        IOptions<DeadpoolSettings> settings,
        ILogger<BackupWorkflow> logger)
    {
        _precheckService = precheckService;
        _backupExecutor = backupExecutor;
        _fileCopyService = fileCopyService;
        _jobRepository = jobRepository;
        _catalogRepository = catalogRepository;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Entry point called by the Quartz job.
    /// Creates a new BackupJob and drives it through the state machine.
    /// </summary>
    public async Task RunAsync(
        DatabaseProfile profile,
        BackupType backupType,
        CancellationToken cancellationToken = default)
    {
        // ── Guard: one active job per database at a time ──────────────────────
        var existing = await _jobRepository.GetActiveJobAsync(profile.DatabaseId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogWarning(
                "Skipping {BackupType} for {Database}: job {JobId} is already active in state {State}",
                backupType, profile.DatabaseName, existing.JobId, existing.State);
            return;
        }

        // ── Create job in Pending state ───────────────────────────────────────
        var job = new BackupJob
        {
            JobId = Guid.NewGuid(),
            DatabaseId = profile.DatabaseId,
            BackupType = backupType,
            ScheduledAt = DateTime.UtcNow,
            State = BackupJobState.Pending
        };
        await _jobRepository.AddAsync(job, cancellationToken);
        _logger.LogInformation("Backup job created: {JobId} | {Database} | {BackupType}",
            job.JobId, profile.DatabaseName, backupType);

        // ── Step 1: Precheck ──────────────────────────────────────────────────
        var precheck = await _precheckService.RunAsync(profile, backupType, cancellationToken);
        if (!precheck.Passed)
        {
            var reason = string.Join("; ", precheck.Failures);
            _logger.LogError("Precheck failed for job {JobId}: {Reason}", job.JobId, reason);
            await FailJobAsync(job.JobId, reason, cancellationToken);
            return;
        }

        // ── Step 2: Execute backup → Running ─────────────────────────────────
        await _jobRepository.UpdateStateAsync(job.JobId, BackupJobState.Running, cancellationToken: cancellationToken);
        job.StartedAt = DateTime.UtcNow;

        string localFilePath;
        try
        {
            localFilePath = await _backupExecutor.ExecuteAsync(profile, backupType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup execution failed for job {JobId}", job.JobId);
            await FailJobAsync(job.JobId, ex.Message, cancellationToken);
            return;
        }

        // ── BackupCompleted ───────────────────────────────────────────────────
        await _jobRepository.UpdateStateAsync(job.JobId, BackupJobState.BackupCompleted, cancellationToken: cancellationToken);
        _logger.LogInformation("Backup file written: {JobId} -> {File}", job.JobId, localFilePath);

        // ── Step 3: Copy to storage (with retry) → Copying ───────────────────
        await _jobRepository.UpdateStateAsync(job.JobId, BackupJobState.Copying, cancellationToken: cancellationToken);

        var catalog = new BackupCatalog
        {
            BackupId = Guid.NewGuid(),
            DatabaseId = profile.DatabaseId,
            BackupType = backupType,
            BackupDate = DateTime.UtcNow,
            BackupFile = localFilePath,
            Status = BackupJobState.Copying,
            CopyAttempts = 0
        };
        await _catalogRepository.AddAsync(catalog, cancellationToken);

        var destinationDir = BuildStoragePath(profile, backupType);
        string? storagePath = null;

        for (var attempt = 1; attempt <= _settings.CopyMaxAttempts; attempt++)
        {
            try
            {
                storagePath = await _fileCopyService.CopyAsync(localFilePath, destinationDir, cancellationToken);
                catalog.CopyAttempts = attempt;
                break; // success — exit retry loop
            }
            catch (Exception ex)
            {
                catalog.CopyAttempts = attempt;
                _logger.LogWarning(ex, "Copy attempt {Attempt}/{Max} failed for job {JobId}",
                    attempt, _settings.CopyMaxAttempts, job.JobId);

                if (attempt < _settings.CopyMaxAttempts)
                {
                    // → RetryPending: wait then try again
                    await _jobRepository.UpdateStateAsync(
                        job.JobId, BackupJobState.RetryPending, ex.Message, cancellationToken);

                    await Task.Delay(
                        TimeSpan.FromSeconds(_settings.CopyRetryDelaySeconds), cancellationToken);

                    // back to Copying for next attempt
                    await _jobRepository.UpdateStateAsync(
                        job.JobId, BackupJobState.Copying, cancellationToken: cancellationToken);
                }
                else
                {
                    // All attempts exhausted → Failed
                    catalog.Status = BackupJobState.Failed;
                    catalog.ErrorMessage = ex.Message;
                    await _catalogRepository.UpdateAsync(catalog, cancellationToken);
                    await FailJobAsync(job.JobId, ex.Message, cancellationToken);
                    return;
                }
            }
        }

        // ── Step 4: Verify copy ───────────────────────────────────────────────
        var verified = await _fileCopyService.VerifyAsync(localFilePath, storagePath!, cancellationToken);
        if (!verified)
        {
            const string verifyError = "Copy verification failed: file size mismatch.";
            _logger.LogError("{Error} JobId={JobId}", verifyError, job.JobId);
            await FailJobAsync(job.JobId, verifyError, cancellationToken);
            return;
        }

        await _jobRepository.UpdateStateAsync(job.JobId, BackupJobState.Verified, cancellationToken: cancellationToken);

        // ── Step 5: Update catalog & mark Success ────────────────────────���────
        catalog.StoragePath = storagePath!;
        catalog.Verified = true;
        catalog.Status = BackupJobState.Success;
        await _catalogRepository.UpdateAsync(catalog, cancellationToken);

        job.CompletedAt = DateTime.UtcNow;
        await _jobRepository.UpdateStateAsync(job.JobId, BackupJobState.Success, cancellationToken: cancellationToken);
        _logger.LogInformation("Backup succeeded: {JobId}", job.JobId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task FailJobAsync(Guid jobId, string reason, CancellationToken cancellationToken)
    {
        _logger.LogError("Backup job failed: {JobId} | {Reason}", jobId, reason);
        await _jobRepository.UpdateStateAsync(jobId, BackupJobState.Failed, reason, cancellationToken);
    }

    private string BuildStoragePath(DatabaseProfile profile, BackupType backupType)
    {
        var subFolder = backupType switch
        {
            BackupType.Full => "FULL",
            BackupType.Differential => "DIFF",
            BackupType.TransactionLog => "LOG",
            _ => "FULL"
        };
        return Path.Combine(_settings.StorageRoot, profile.DatabaseName, subFolder);
    }
}

