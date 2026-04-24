using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;
using Deadpool.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Deadpool.Core.Workflow;

/// <summary>
/// Orchestrates the full backup workflow state machine.
///
/// State transitions:
///   Pending → Running → BackupCompleted → Copying → Verified → Success
///                                                 ↘ RetryPending (up to 3x) → Failed
///            ↘ Failed (precheck or backup failure – no automatic retry)
/// </summary>
public sealed class BackupWorkflow
{
    private const int MaxCopyAttempts = 3;

    private readonly IPrecheckService _precheckService;
    private readonly IBackupExecutor _backupExecutor;
    private readonly IFileCopyService _fileCopyService;
    private readonly IBackupJobRepository _jobRepository;
    private readonly IBackupCatalogRepository _catalogRepository;
    private readonly ILogger<BackupWorkflow> _logger;

    public BackupWorkflow(
        IPrecheckService precheckService,
        IBackupExecutor backupExecutor,
        IFileCopyService fileCopyService,
        IBackupJobRepository jobRepository,
        IBackupCatalogRepository catalogRepository,
        ILogger<BackupWorkflow> logger)
    {
        _precheckService = precheckService;
        _backupExecutor = backupExecutor;
        _fileCopyService = fileCopyService;
        _jobRepository = jobRepository;
        _catalogRepository = catalogRepository;
        _logger = logger;
    }

    /// <summary>
    /// Entry point called by the Quartz job.
    /// Creates a new BackupJob and drives it through the state machine.
    /// </summary>
    public async Task RunAsync(DatabaseProfile profile, BackupType backupType, CancellationToken cancellationToken = default)
    {
        // Guard: one active job per database
        var existing = await _jobRepository.GetActiveJobAsync(profile.DatabaseId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogWarning(
                "Skipping {BackupType} for {Database}: job {JobId} is already active in state {State}",
                backupType, profile.DatabaseName, existing.JobId, existing.State);
            return;
        }

        var job = new BackupJob
        {
            JobId = Guid.NewGuid(),
            DatabaseId = profile.DatabaseId,
            BackupType = backupType,
            ScheduledAt = DateTime.UtcNow,
            State = BackupJobState.Pending
        };

        await _jobRepository.AddAsync(job, cancellationToken);
        _logger.LogInformation("Backup started: {JobId} | {Database} | {BackupType}", job.JobId, profile.DatabaseName, backupType);

        // --- Step 1: Precheck ---
        var precheck = await _precheckService.RunAsync(profile, backupType, cancellationToken);
        if (!precheck.Passed)
        {
            await FailJobAsync(job.JobId, string.Join("; ", precheck.Failures), cancellationToken);
            return;
        }

        // --- Step 2: Execute backup ---
        await _jobRepository.UpdateStateAsync(job.JobId, BackupJobState.Running, cancellationToken: cancellationToken);
        job.StartedAt = DateTime.UtcNow;

        string localFilePath;
        try
        {
            // TODO: implement in IBackupExecutor
            localFilePath = await _backupExecutor.ExecuteAsync(profile, backupType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup execution failed for job {JobId}", job.JobId);
            await FailJobAsync(job.JobId, ex.Message, cancellationToken);
            return;
        }

        await _jobRepository.UpdateStateAsync(job.JobId, BackupJobState.BackupCompleted, cancellationToken: cancellationToken);
        _logger.LogInformation("Backup completed: {JobId} -> {File}", job.JobId, localFilePath);

        // --- Step 3: Copy to storage (with retry) ---
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

        string? storagePath = null;
        for (var attempt = 1; attempt <= MaxCopyAttempts; attempt++)
        {
            try
            {
                // TODO: resolve destination path from configuration / StoragePathResolver
                var destinationRoot = string.Empty; // placeholder
                storagePath = await _fileCopyService.CopyAsync(localFilePath, destinationRoot, MaxCopyAttempts, cancellationToken);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Copy retry {Attempt}/{Max} for job {JobId}", attempt, MaxCopyAttempts, job.JobId);
                catalog.CopyAttempts = attempt;

                if (attempt < MaxCopyAttempts)
                {
                    await _jobRepository.UpdateStateAsync(job.JobId, BackupJobState.RetryPending, ex.Message, cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
                else
                {
                    await FailJobAsync(job.JobId, ex.Message, cancellationToken);
                    catalog.Status = BackupJobState.Failed;
                    catalog.ErrorMessage = ex.Message;
                    await _catalogRepository.UpdateAsync(catalog, cancellationToken);
                    return;
                }
            }
        }

        // --- Step 4: Verify copy ---
        var verified = await _fileCopyService.VerifyAsync(localFilePath, storagePath!, cancellationToken);
        if (!verified)
        {
            const string verifyError = "Copy verification failed: file size mismatch.";
            _logger.LogError("{Error} JobId={JobId}", verifyError, job.JobId);
            await FailJobAsync(job.JobId, verifyError, cancellationToken);
            return;
        }

        await _jobRepository.UpdateStateAsync(job.JobId, BackupJobState.Verified, cancellationToken: cancellationToken);

        // --- Step 5: Update catalog & mark success ---
        catalog.StoragePath = storagePath!;
        catalog.Verified = true;
        catalog.Status = BackupJobState.Success;
        await _catalogRepository.UpdateAsync(catalog, cancellationToken);

        job.CompletedAt = DateTime.UtcNow;
        await _jobRepository.UpdateStateAsync(job.JobId, BackupJobState.Success, cancellationToken: cancellationToken);
        _logger.LogInformation("Backup succeeded: {JobId}", job.JobId);
    }

    // -------------------------------------------------------------------------

    private async Task FailJobAsync(Guid jobId, string reason, CancellationToken cancellationToken)
    {
        _logger.LogError("Backup failed: {JobId} | {Reason}", jobId, reason);
        await _jobRepository.UpdateStateAsync(jobId, BackupJobState.Failed, reason, cancellationToken);
    }
}

