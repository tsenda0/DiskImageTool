
namespace DiskImageTool;

public interface IFileSystem : IDisposable
{
    string FileSystemType { get; }
    int ImageSizeBytes { get; }

    IFileEntry GetRoot(bool isUTC = false);
    Stream OpenFile(IFileEntry file);
    Stream OpenFile(string path);
}