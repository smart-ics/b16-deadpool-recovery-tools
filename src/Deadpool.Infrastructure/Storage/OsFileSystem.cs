namespace Deadpool.Infrastructure.Storage;

/// <summary>
/// Production implementation wrapping System.IO.
/// All file operations go through real OS filesystem.
/// </summary>
internal sealed class OsFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public long GetFileSize(string path) => new FileInfo(path).Length;

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void CopyFile(string sourceFilePath, string destFilePath, bool overwrite = true) =>
        File.Copy(sourceFilePath, destFilePath, overwrite);
}

