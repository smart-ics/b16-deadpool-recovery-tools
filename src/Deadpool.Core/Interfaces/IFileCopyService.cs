namespace Deadpool.Core.Interfaces;

/// <summary>
/// Copies a backup file from local storage to the backup storage server.
/// Implementation lives in Deadpool.Infrastructure.
/// </summary>
public interface IFileCopyService
{
    /// <summary>
    /// Copies <paramref name="sourceFilePath"/> to <paramref name="destinationPath"/>.
    /// Retries up to <paramref name="maxAttempts"/> times on failure.
    /// Returns the full destination path on success.
    /// </summary>
    Task<string> CopyAsync(
        string sourceFilePath,
        string destinationPath,
        int maxAttempts = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that the copied file exists and its size matches the source.
    /// </summary>
    Task<bool> VerifyAsync(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default);
}

