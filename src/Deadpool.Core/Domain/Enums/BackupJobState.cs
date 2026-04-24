namespace Deadpool.Core.Domain.Enums;

public enum BackupJobState
{
    Pending,
    Running,
    BackupCompleted,
    Copying,
    Verified,
    Success,
    Failed,
    RetryPending
}

