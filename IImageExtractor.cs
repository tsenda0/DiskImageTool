
namespace DiskImageTool;

public interface IImageExtractor : IDisposable
{
    FatFileSystem? FileSystem { get; }

    void ExtractFile(FatFileEntry file, string path);

    bool OpenImage(string file);
    bool OpenImage(Stream stream);

    FatFileEntry? GetRoot(bool isUTC);
}
