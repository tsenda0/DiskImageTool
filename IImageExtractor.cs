
namespace DiskImageTool;

public interface IImageExtractor : IDisposable
{
    FatFileSystem? FileSystem { get; }

    void ExtractFile(ImageFile file, string path);

    void OpenImage(string file);
    void OpenImage(Stream stream);

    ImageFile? GetRoot(bool isUTC);
}
