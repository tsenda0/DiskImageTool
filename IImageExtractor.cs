
namespace DiskImageTool;

public interface IImageExtractor : IDisposable
{
    FatFileSystem? FileSystem { get; }

    void ExtractFile(ImageFile file, string path);

    bool OpenImage(string file);
    bool OpenImage(Stream stream);

    ImageFile? GetRoot(bool isUTC);
}
