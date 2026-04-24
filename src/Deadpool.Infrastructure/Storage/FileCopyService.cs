using Deadpool.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Deadpool.Infrastructure.Storage;

/// <summary>
/// Copies backup files to the backup storage server using local file I/O.
/// Never backs up directly to the network share (copy-after-local strategy).
/// Phase-1 skeleton — full implementation pending.
/// </summary>
internal sealed class FileCopyService : IFileCopyService
{
    private readonly ILogger<FileCopyService> _logger;

    public FileCopyService(ILogger<FileCopyService> logger)
    {
        _logger = logger;
    }

    public Task<string> CopyAsync(
        string sourceFilePath,
        string destinationPath,
        int maxAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement File.Copy / stream copy to UNC path
        // Destination layout: \\Backup\HospitalA\FULL | DIFF | LOG
        throw new NotImplementedException("File copy is not yet implemented.");
    }

    public Task<bool> VerifyAsync(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default)
    {
        // TODO: Compare FileInfo.Length of source vs destination
        throw new NotImplementedException("File verification is not yet implemented.");
    }
}

