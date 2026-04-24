namespace Deadpool.Infrastructure.Storage;

/// <summary>
/// Abstracts System.IO operations so FileCopyService can be unit-tested
/// without touching the real filesystem.
/// </summary>
internal interface IFileSystem
{
    bool   FileExists(string path);
    long   GetFileSize(string path);
    void   CreateDirectory(string path);
    void   CopyFile(string sourceFilePath, string destFilePath, bool overwrite = true);
}

