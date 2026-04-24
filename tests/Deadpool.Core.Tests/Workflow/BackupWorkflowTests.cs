using Deadpool.Core.Configuration;
using Deadpool.Core.Domain.Entities;
using Deadpool.Core.Domain.Enums;
using Deadpool.Core.Interfaces;
using Deadpool.Core.Workflow;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Deadpool.Core.Tests.Workflow;

/// <summary>
/// Unit tests for BackupWorkflow state machine.
/// Focuses purely on orchestration logic — all I/O is mocked.
///
/// Naming convention: MethodName_ShouldExpectedResult_WhenCondition
/// Pattern: Arrange / Act / Assert
/// </summary>
public sealed class BackupWorkflowTests
{
    // ── Mocks ─────────────────────────────────────────────────────────────────

    private readonly Mock<IPrecheckService> _precheckService = new();
    private readonly Mock<IBackupExecutor> _backupExecutor = new();
    private readonly Mock<IFileCopyService> _fileCopyService = new();
    private readonly Mock<IBackupJobRepository> _jobRepository = new();
    private readonly Mock<IBackupCatalogRepository> _catalogRepository = new();

    // ── Common test fixtures ──────────────────────────────────────────────────

    private static readonly DatabaseProfile AnyProfile = new()
    {
        DatabaseId = Guid.NewGuid(),
        DatabaseName = "HospitalA",
        RecoveryModel = RecoveryModel.Full
    };

    private const string LocalFilePath = @"C:\Deadpool\Backups\HospitalA_FULL_20260424_020000.bak";
    private const string StoragePath   = @"\\BackupServer\Backup\HospitalA\FULL\HospitalA_FULL_20260424_020000.bak";

    // Settings with zero retry delay so tests don't hang
    private static IOptions<DeadpoolSettings> FastSettings => Options.Create(new DeadpoolSettings
    {
        StorageRoot           = @"\\BackupServer\Backup",
        CopyMaxAttempts       = 3,
        CopyRetryDelaySeconds = 0
    });

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Captures every BackupJobState passed to UpdateStateAsync in call order.
    /// </summary>
    private List<BackupJobState> CaptureStateTransitions()
    {
        var states = new List<BackupJobState>();
        _jobRepository
            .Setup(r => r.UpdateStateAsync(
                It.IsAny<Guid>(),
                It.IsAny<BackupJobState>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, BackupJobState, string?, CancellationToken>((_, s, _, _) => states.Add(s))
            .Returns(Task.CompletedTask);
        return states;
    }

    private BackupWorkflow BuildWorkflow() => new(
        _precheckService.Object,
        _backupExecutor.Object,
        _fileCopyService.Object,
        _jobRepository.Object,
        _catalogRepository.Object,
        FastSettings,
        NullLogger<BackupWorkflow>.Instance);

    // ═════════════════════════════════════════════════════════════════════════
    // Test cases
    // ═════════════════════════════════════════════════════════════════════════

    // ── 1. Happy path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_ShouldTransitionToSuccess_WhenAllStepsSucceed()
    {
        // Arrange
        var states = CaptureStateTransitions();

        _jobRepository.Setup(r => r.GetActiveJobAsync(AnyProfile.DatabaseId, default))
            .ReturnsAsync((BackupJob?)null);
        _jobRepository.Setup(r => r.AddAsync(It.IsAny<BackupJob>(), default))
            .Returns(Task.CompletedTask);

        _precheckService.Setup(p => p.RunAsync(AnyProfile, BackupType.Full, default))
            .ReturnsAsync(PrecheckResult.Success());

        _backupExecutor.Setup(e => e.ExecuteAsync(AnyProfile, BackupType.Full, default))
            .ReturnsAsync(LocalFilePath);

        _fileCopyService.Setup(f => f.CopyAsync(LocalFilePath, It.IsAny<string>(), default))
            .ReturnsAsync(StoragePath);
        _fileCopyService.Setup(f => f.VerifyAsync(LocalFilePath, StoragePath, default))
            .ReturnsAsync(true);

        _catalogRepository.Setup(c => c.AddAsync(It.IsAny<BackupCatalog>(), default))
            .Returns(Task.CompletedTask);
        _catalogRepository.Setup(c => c.UpdateAsync(It.IsAny<BackupCatalog>(), default))
            .Returns(Task.CompletedTask);

        var workflow = BuildWorkflow();

        // Act
        await workflow.RunAsync(AnyProfile, BackupType.Full);

        // Assert — exact state machine sequence
        states.Should().ContainInOrder(
            BackupJobState.Running,
            BackupJobState.BackupCompleted,
            BackupJobState.Copying,
            BackupJobState.Verified,
            BackupJobState.Success);

        // Catalog must be persisted once and updated to Success
        _catalogRepository.Verify(c => c.AddAsync(It.IsAny<BackupCatalog>(), default), Times.Once);
        _catalogRepository.Verify(c => c.UpdateAsync(
            It.Is<BackupCatalog>(cat => cat.Status == BackupJobState.Success && cat.Verified),
            default), Times.Once);
    }

    // ── 2. Precheck fails ────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_ShouldTransitionToFailed_WhenPrecheckFails()
    {
        // Arrange
        var states = CaptureStateTransitions();

        _jobRepository.Setup(r => r.GetActiveJobAsync(AnyProfile.DatabaseId, default))
            .ReturnsAsync((BackupJob?)null);
        _jobRepository.Setup(r => r.AddAsync(It.IsAny<BackupJob>(), default))
            .Returns(Task.CompletedTask);

        _precheckService.Setup(p => p.RunAsync(AnyProfile, BackupType.Full, default))
            .ReturnsAsync(PrecheckResult.Fail("Database offline", "Insufficient disk space"));

        var workflow = BuildWorkflow();

        // Act
        await workflow.RunAsync(AnyProfile, BackupType.Full);

        // Assert — must go straight to Failed, never enter Running
        states.Should().ContainSingle()
            .Which.Should().Be(BackupJobState.Failed);

        _backupExecutor.Verify(e => e.ExecuteAsync(It.IsAny<DatabaseProfile>(), It.IsAny<BackupType>(), default),
            Times.Never);
    }

    // ── 3. Backup executor throws ────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_ShouldTransitionToFailed_WhenBackupExecutorThrows()
    {
        // Arrange
        var states = CaptureStateTransitions();

        _jobRepository.Setup(r => r.GetActiveJobAsync(AnyProfile.DatabaseId, default))
            .ReturnsAsync((BackupJob?)null);
        _jobRepository.Setup(r => r.AddAsync(It.IsAny<BackupJob>(), default))
            .Returns(Task.CompletedTask);

        _precheckService.Setup(p => p.RunAsync(AnyProfile, BackupType.Full, default))
            .ReturnsAsync(PrecheckResult.Success());

        _backupExecutor.Setup(e => e.ExecuteAsync(AnyProfile, BackupType.Full, default))
            .ThrowsAsync(new InvalidOperationException("SQL Server unavailable"));

        var workflow = BuildWorkflow();

        // Act
        await workflow.RunAsync(AnyProfile, BackupType.Full);

        // Assert — Running then Failed, no copy attempted
        states.Should().ContainInOrder(BackupJobState.Running, BackupJobState.Failed);
        states.Should().NotContain(BackupJobState.Copying);

        _fileCopyService.Verify(f => f.CopyAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    // ── 4. Copy fails once then succeeds ─────────────────────────────────────

    [Fact]
    public async Task RunAsync_ShouldTransitionToSuccess_WhenCopyFailsOnceThenSucceeds()
    {
        // Arrange
        var states = CaptureStateTransitions();

        _jobRepository.Setup(r => r.GetActiveJobAsync(AnyProfile.DatabaseId, default))
            .ReturnsAsync((BackupJob?)null);
        _jobRepository.Setup(r => r.AddAsync(It.IsAny<BackupJob>(), default))
            .Returns(Task.CompletedTask);

        _precheckService.Setup(p => p.RunAsync(AnyProfile, BackupType.Full, default))
            .ReturnsAsync(PrecheckResult.Success());
        _backupExecutor.Setup(e => e.ExecuteAsync(AnyProfile, BackupType.Full, default))
            .ReturnsAsync(LocalFilePath);

        // First call throws, second call succeeds
        _fileCopyService.SetupSequence(f => f.CopyAsync(LocalFilePath, It.IsAny<string>(), default))
            .ThrowsAsync(new IOException("Network blip"))
            .ReturnsAsync(StoragePath);

        _fileCopyService.Setup(f => f.VerifyAsync(LocalFilePath, StoragePath, default))
            .ReturnsAsync(true);

        _catalogRepository.Setup(c => c.AddAsync(It.IsAny<BackupCatalog>(), default))
            .Returns(Task.CompletedTask);
        _catalogRepository.Setup(c => c.UpdateAsync(It.IsAny<BackupCatalog>(), default))
            .Returns(Task.CompletedTask);

        var workflow = BuildWorkflow();

        // Act
        await workflow.RunAsync(AnyProfile, BackupType.Full);

        // Assert — RetryPending appears once, then Copying resumes, ends in Success
        states.Should().ContainInOrder(
            BackupJobState.Running,
            BackupJobState.BackupCompleted,
            BackupJobState.Copying,
            BackupJobState.RetryPending,
            BackupJobState.Copying,   // retry
            BackupJobState.Verified,
            BackupJobState.Success);

        // CopyAsync was called twice
        _fileCopyService.Verify(f => f.CopyAsync(LocalFilePath, It.IsAny<string>(), default), Times.Exactly(2));
    }

    // ── 5. All copy attempts fail ─────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_ShouldTransitionToFailed_WhenAllCopyAttemptsFail()
    {
        // Arrange
        var states = CaptureStateTransitions();

        _jobRepository.Setup(r => r.GetActiveJobAsync(AnyProfile.DatabaseId, default))
            .ReturnsAsync((BackupJob?)null);
        _jobRepository.Setup(r => r.AddAsync(It.IsAny<BackupJob>(), default))
            .Returns(Task.CompletedTask);

        _precheckService.Setup(p => p.RunAsync(AnyProfile, BackupType.Full, default))
            .ReturnsAsync(PrecheckResult.Success());
        _backupExecutor.Setup(e => e.ExecuteAsync(AnyProfile, BackupType.Full, default))
            .ReturnsAsync(LocalFilePath);

        // All 3 attempts throw
        _fileCopyService.Setup(f => f.CopyAsync(LocalFilePath, It.IsAny<string>(), default))
            .ThrowsAsync(new IOException("Storage unreachable"));

        _catalogRepository.Setup(c => c.AddAsync(It.IsAny<BackupCatalog>(), default))
            .Returns(Task.CompletedTask);
        _catalogRepository.Setup(c => c.UpdateAsync(It.IsAny<BackupCatalog>(), default))
            .Returns(Task.CompletedTask);

        var workflow = BuildWorkflow();

        // Act
        await workflow.RunAsync(AnyProfile, BackupType.Full);

        // Assert — RetryPending twice (after attempt 1 and 2), then Failed
        states.Should().ContainInOrder(
            BackupJobState.Running,
            BackupJobState.BackupCompleted,
            BackupJobState.Copying,
            BackupJobState.RetryPending,
            BackupJobState.Copying,
            BackupJobState.RetryPending,
            BackupJobState.Copying,
            BackupJobState.Failed);

        // CopyAsync was called exactly 3 times
        _fileCopyService.Verify(f => f.CopyAsync(LocalFilePath, It.IsAny<string>(), default), Times.Exactly(3));

        // Catalog updated with Failed status
        _catalogRepository.Verify(c => c.UpdateAsync(
            It.Is<BackupCatalog>(cat => cat.Status == BackupJobState.Failed && cat.CopyAttempts == 3),
            default), Times.Once);
    }

    // ── 6. Verification fails ─────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_ShouldTransitionToFailed_WhenVerificationFails()
    {
        // Arrange
        var states = CaptureStateTransitions();

        _jobRepository.Setup(r => r.GetActiveJobAsync(AnyProfile.DatabaseId, default))
            .ReturnsAsync((BackupJob?)null);
        _jobRepository.Setup(r => r.AddAsync(It.IsAny<BackupJob>(), default))
            .Returns(Task.CompletedTask);

        _precheckService.Setup(p => p.RunAsync(AnyProfile, BackupType.Full, default))
            .ReturnsAsync(PrecheckResult.Success());
        _backupExecutor.Setup(e => e.ExecuteAsync(AnyProfile, BackupType.Full, default))
            .ReturnsAsync(LocalFilePath);

        _fileCopyService.Setup(f => f.CopyAsync(LocalFilePath, It.IsAny<string>(), default))
            .ReturnsAsync(StoragePath);
        // Verify returns false — size mismatch
        _fileCopyService.Setup(f => f.VerifyAsync(LocalFilePath, StoragePath, default))
            .ReturnsAsync(false);

        _catalogRepository.Setup(c => c.AddAsync(It.IsAny<BackupCatalog>(), default))
            .Returns(Task.CompletedTask);

        var workflow = BuildWorkflow();

        // Act
        await workflow.RunAsync(AnyProfile, BackupType.Full);

        // Assert — Verified state never reached, ends in Failed
        states.Should().NotContain(BackupJobState.Verified);
        states.Should().NotContain(BackupJobState.Success);
        states.Last().Should().Be(BackupJobState.Failed);
    }

    // ── 7. Concurrent job guard ───────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_ShouldSkipExecution_WhenActiveJobAlreadyExists()
    {
        // Arrange
        var existingJob = new BackupJob
        {
            JobId      = Guid.NewGuid(),
            DatabaseId = AnyProfile.DatabaseId,
            State      = BackupJobState.Running
        };

        _jobRepository.Setup(r => r.GetActiveJobAsync(AnyProfile.DatabaseId, default))
            .ReturnsAsync(existingJob);

        var workflow = BuildWorkflow();

        // Act
        await workflow.RunAsync(AnyProfile, BackupType.Full);

        // Assert — no new job created, no state transitions, no backup attempted
        _jobRepository.Verify(r => r.AddAsync(It.IsAny<BackupJob>(), default), Times.Never);
        _jobRepository.Verify(r => r.UpdateStateAsync(
            It.IsAny<Guid>(), It.IsAny<BackupJobState>(), It.IsAny<string?>(), default), Times.Never);
        _precheckService.Verify(p => p.RunAsync(It.IsAny<DatabaseProfile>(), It.IsAny<BackupType>(), default),
            Times.Never);
        _backupExecutor.Verify(e => e.ExecuteAsync(It.IsAny<DatabaseProfile>(), It.IsAny<BackupType>(), default),
            Times.Never);
    }
}

