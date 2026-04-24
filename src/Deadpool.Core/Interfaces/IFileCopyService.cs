namespace Deadpool.Core.Interfaces;

/// <summary>
/// Copies a backup file from local storage to the backup storage server.
/// A single call = one attempt. Retry policy is owned by BackupWorkflow.
/// Implementation lives in Deadpool.Infrastructure.
/// </summary>
public interface IFileCopyService
{
    /// <summary>
    /// Performs a single copy attempt of <paramref name="sourceFilePath"/> to
    /// <paramref name="destinationDirectory"/>. Throws on failure.
    /// Returns the full destination file path on success.
    /// </summary>
    Task<string> CopyAsync(
        string sourceFilePath,
        string destinationDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that the copied file exists and its size matches the source.
    /// </summary>
    Task<bool> VerifyAsync(
        string sourceFilePath,
        string destinationFilePath,
        CancellationToken cancellationToken = default);
}

