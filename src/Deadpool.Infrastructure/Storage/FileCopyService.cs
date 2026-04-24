using Deadpool.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Deadpool.Infrastructure.Storage;

/// <summary>
/// Copies backup files to the backup storage server using local/UNC file I/O.
/// Never backs up directly to the network share (copy-after-local strategy).
///
/// Workflow per CopyAsync call:
///   1. Validate source exists and size > 0
///   2. Ensure destination directory exists
///   3. Copy file (overwrite if exists)
///   4. Return full destination path
///
/// VerifyAsync compares source and destination file sizes.
/// Retry logic lives in BackupWorkflow, not here.
/// </summary>
internal sealed class FileCopyService : IFileCopyService
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FileCopyService> _logger;

    public FileCopyService(IFileSystem fileSystem, ILogger<FileCopyService> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public Task<string> CopyAsync(
        string sourceFilePath,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        // ── Step 1: Validate source ──────────────────────────────────────────
        if (!_fileSystem.FileExists(sourceFilePath))
        {
            _logger.LogError("Source file not found: {SourcePath}", sourceFilePath);
            throw new FileNotFoundException($"Source backup file not found: {sourceFilePath}", sourceFilePath);
        }

        var sourceSize = _fileSystem.GetFileSize(sourceFilePath);
        if (sourceSize == 0)
        {
            _logger.LogWarning("Source file is empty (0 bytes): {SourcePath}", sourceFilePath);
            throw new InvalidOperationException($"Source backup file is empty (0 bytes): {sourceFilePath}");
        }

        // ── Step 2: Build destination path & ensure directory exists ─────────
        var fileName = Path.GetFileName(sourceFilePath);
        var destFilePath = Path.Combine(destinationDirectory, fileName);

        _fileSystem.CreateDirectory(destinationDirectory);

        _logger.LogInformation(
            "Starting file copy: {SourcePath} → {DestinationPath} ({FileSizeBytes} bytes)",
            sourceFilePath, destFilePath, sourceSize);

        // ── Step 3: Copy ─────────────────────────────────────────────────────
        _fileSystem.CopyFile(sourceFilePath, destFilePath, overwrite: true);

        _logger.LogInformation(
            "File copy completed: {DestinationPath} ({FileSizeBytes} bytes)",
            destFilePath, sourceSize);

        return Task.FromResult(destFilePath);
    }

    public Task<bool> VerifyAsync(
        string sourceFilePath,
        string destinationFilePath,
        CancellationToken cancellationToken = default)
    {
        // ── Check destination exists ─────────────────────────────────────────
        if (!_fileSystem.FileExists(destinationFilePath))
        {
            _logger.LogWarning(
                "Verification failed: destination file not found. {DestinationPath}",
                destinationFilePath);
            return Task.FromResult(false);
        }

        // ── Compare sizes ────────────────────────────────────────────────────
        var sourceSize = _fileSystem.GetFileSize(sourceFilePath);
        var destSize = _fileSystem.GetFileSize(destinationFilePath);

        var verified = sourceSize == destSize;

        if (verified)
        {
            _logger.LogInformation(
                "Verification passed: sizes match. {SourceSize} == {DestinationSize} bytes",
                sourceSize, destSize);
        }
        else
        {
            _logger.LogWarning(
                "Verification failed: size mismatch. Source={SourceSize} bytes, Destination={DestinationSize} bytes",
                sourceSize, destSize);
        }

        return Task.FromResult(verified);
    }
}

