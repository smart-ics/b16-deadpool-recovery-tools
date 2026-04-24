using Deadpool.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Deadpool.Infrastructure.Tests.Storage;

/// <summary>
/// Unit tests for FileCopyService.
/// Uses mocked IFileSystem — no real filesystem I/O during tests.
///
/// Naming: MethodName_ShouldExpectedResult_WhenCondition
/// Pattern: Arrange / Act / Assert
/// </summary>
public sealed class FileCopyServiceTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private const string SourceFilePath      = @"C:\Deadpool\Backups\DB_FULL_20260424_020000.bak";
    private const string DestinationDir      = @"\\BackupServer\Backup\HospitalA\FULL";
    private const string ExpectedDestPath    = @"\\BackupServer\Backup\HospitalA\FULL\DB_FULL_20260424_020000.bak";
    private const long   ValidFileSize       = 12345678;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FileCopyService BuildService(Mock<IFileSystem> fileSystemMock) =>
        new(fileSystemMock.Object, NullLogger<FileCopyService>.Instance);

    // ══════════════════════════════════════════════════════════════════════════
    // CopyAsync tests
    // ══════════════════════════════════════════════════════════════════════════

    // ── 1. Happy path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CopyAsync_ShouldReturnDestinationFilePath_WhenCopySucceeds()
    {
        // Arrange
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.FileExists(SourceFilePath)).Returns(true);
        fileSystemMock.Setup(fs => fs.GetFileSize(SourceFilePath)).Returns(ValidFileSize);
        fileSystemMock.Setup(fs => fs.CreateDirectory(DestinationDir));
        fileSystemMock.Setup(fs => fs.CopyFile(SourceFilePath, ExpectedDestPath, true));

        var service = BuildService(fileSystemMock);

        // Act
        var result = await service.CopyAsync(SourceFilePath, DestinationDir);

        // Assert
        result.Should().Be(ExpectedDestPath);
    }

    // ── 2. Directory creation ─────────────────────────────────────────────────

    [Fact]
    public async Task CopyAsync_ShouldCreateDestinationDirectory_BeforeCopying()
    {
        // Arrange
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.FileExists(SourceFilePath)).Returns(true);
        fileSystemMock.Setup(fs => fs.GetFileSize(SourceFilePath)).Returns(ValidFileSize);

        var service = BuildService(fileSystemMock);

        // Act
        await service.CopyAsync(SourceFilePath, DestinationDir);

        // Assert — CreateDirectory called once with exact destination directory
        fileSystemMock.Verify(
            fs => fs.CreateDirectory(DestinationDir),
            Times.Once);
    }

    // ── 3. Source file not found ──────────────────────────────────────────────

    [Fact]
    public async Task CopyAsync_ShouldThrowFileNotFoundException_WhenSourceDoesNotExist()
    {
        // Arrange
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.FileExists(SourceFilePath)).Returns(false);

        var service = BuildService(fileSystemMock);

        // Act
        Func<Task> act = async () => await service.CopyAsync(SourceFilePath, DestinationDir);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage($"*{SourceFilePath}*");

        // CopyFile should never be called if source doesn't exist
        fileSystemMock.Verify(
            fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    // ── 4. Zero-byte source file ──────────────────────────────────────────────

    [Fact]
    public async Task CopyAsync_ShouldThrowInvalidOperationException_WhenSourceFileSizeIsZero()
    {
        // Arrange
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.FileExists(SourceFilePath)).Returns(true);
        fileSystemMock.Setup(fs => fs.GetFileSize(SourceFilePath)).Returns(0);

        var service = BuildService(fileSystemMock);

        // Act
        Func<Task> act = async () => await service.CopyAsync(SourceFilePath, DestinationDir);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*0 bytes*");

        // CopyFile should never be called for empty files
        fileSystemMock.Verify(
            fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    // ── 5. Overwrite flag ─────────────────────────────────────────────────────

    [Fact]
    public async Task CopyAsync_ShouldCallCopyFileWithOverwriteTrue_WhenCopying()
    {
        // Arrange
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.FileExists(SourceFilePath)).Returns(true);
        fileSystemMock.Setup(fs => fs.GetFileSize(SourceFilePath)).Returns(ValidFileSize);

        var service = BuildService(fileSystemMock);

        // Act
        await service.CopyAsync(SourceFilePath, DestinationDir);

        // Assert — overwrite must always be true per architecture decision
        fileSystemMock.Verify(
            fs => fs.CopyFile(SourceFilePath, ExpectedDestPath, true),
            Times.Once);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // VerifyAsync tests
    // ══════════════════════════════════════════════════════════════════════════

    // ── 6. Verification success (sizes match) ─────────────────────────────────

    [Fact]
    public async Task VerifyAsync_ShouldReturnTrue_WhenBothFileSizesMatch()
    {
        // Arrange
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.FileExists(ExpectedDestPath)).Returns(true);
        fileSystemMock.Setup(fs => fs.GetFileSize(SourceFilePath)).Returns(ValidFileSize);
        fileSystemMock.Setup(fs => fs.GetFileSize(ExpectedDestPath)).Returns(ValidFileSize);

        var service = BuildService(fileSystemMock);

        // Act
        var result = await service.VerifyAsync(SourceFilePath, ExpectedDestPath);

        // Assert
        result.Should().BeTrue();
    }

    // ── 7. Verification failure (size mismatch) ───────────────────────────────

    [Fact]
    public async Task VerifyAsync_ShouldReturnFalse_WhenDestinationSizeDiffers()
    {
        // Arrange
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.FileExists(ExpectedDestPath)).Returns(true);
        fileSystemMock.Setup(fs => fs.GetFileSize(SourceFilePath)).Returns(ValidFileSize);
        fileSystemMock.Setup(fs => fs.GetFileSize(ExpectedDestPath)).Returns(9999); // different

        var service = BuildService(fileSystemMock);

        // Act
        var result = await service.VerifyAsync(SourceFilePath, ExpectedDestPath);

        // Assert
        result.Should().BeFalse();
    }

    // ── 8. Verification failure (destination missing) ─────────────────────────

    [Fact]
    public async Task VerifyAsync_ShouldReturnFalse_WhenDestinationFileDoesNotExist()
    {
        // Arrange
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.FileExists(ExpectedDestPath)).Returns(false);

        var service = BuildService(fileSystemMock);

        // Act
        var result = await service.VerifyAsync(SourceFilePath, ExpectedDestPath);

        // Assert
        result.Should().BeFalse();

        // GetFileSize should never be called on a non-existent destination
        fileSystemMock.Verify(
            fs => fs.GetFileSize(ExpectedDestPath),
            Times.Never);
    }
}

