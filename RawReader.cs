namespace DiskImageTool;

public class RawReader : IImageReader
{
    byte[]? image;

    /// <summary>
    /// disk buffer
    /// </summary>
    const int BUFSIZE = 1440 * 1024;

    static byte[] readRawImage(Stream filestream)
    {
        var buffer = new byte[BUFSIZE];

        using var workMemStream = new MemoryStream();
        int nread;
        do
        {
            nread = filestream.Read(buffer, 0, buffer.Length);
            if (nread == 0) break;

            workMemStream.Write(buffer, 0, nread);

            //Debug.WriteLine($"{file}: read {nread} bytes");
        } while (nread > 0);

        return workMemStream.ToArray();
    }

    public bool OpenImage(Stream stream)
    {
        image = readRawImage(stream);

        return true;
    }

    public bool OpenImage(string file)
    {
        using var stream = new FileStream(file, FileMode.Open);
        return OpenImage(stream);
    }

    public byte[]? GetBuffer()
    {
        return image;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
