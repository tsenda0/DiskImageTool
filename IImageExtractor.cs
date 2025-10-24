
namespace DiskImageTool;

public interface IImageExtractor : IDisposable
{
    FatFileSystem? FileSystem { get; }

    void ExtractFile(FatFile file, string path);

    void OpenImage(string file);

    FatFile? GetRoot(bool isUTC);
}
