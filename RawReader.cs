
using System.IO;

namespace DiskImageTool;
public class RawReader : IImageReader
{
    byte[]? image;

    static byte[] readRawImage(Stream filestream)
    {
        using var workMemStream = new MemoryStream();
        filestream.CopyTo(workMemStream);
        return workMemStream.ToArray();
    }

    public bool OpenImage(Stream stream)
    {
        image = readRawImage(stream);
        OpenFileName = "(stream)";
        return true;
    }

    public bool OpenImage(string file)
    {
        using var stream = new FileStream(file, FileMode.Open);
        var ret = OpenImage(stream);
        OpenFileName = Path.GetFileName(file);
        return ret;
    }

    public string OpenFileName { get; private set; } = "";

    public byte[]? GetBuffer()
    {
        return image;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    bool isDisposed;

    protected virtual void Dispose(bool disposing)
    {
        if (isDisposed) return;

        if (disposing)
        {
            // dispose managed resource
        }

        // dispose unmanaged resource

        isDisposed = true;
    }
}
