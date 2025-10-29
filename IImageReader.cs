
namespace DiskImageTool;

public interface IImageReader : IDisposable
{
    byte[]? GetBuffer();
    bool OpenImage(string file);
    bool OpenImage(Stream stream);
}
