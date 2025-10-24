namespace DiskImageTool;

public class RawExtractor : IImageExtractor
{
    /// <summary>
    /// disk buffer
    /// </summary>
    const int BUFSIZE = 1440 * 1024;

    /// <summary>
    /// イメージ内のFATファイルシステム
    /// </summary>
    public FatFileSystem? FileSystem { get; private set; }

    public void ExtractFile(FatFile file, string path)
    {
        if (FileSystem == null) throw new InvalidOperationException("DCUファイルが選択されていません");

        var buffer = new byte[BUFSIZE];

        using var filestream = FileSystem.OpenFile(file);
        var filename = Path.Combine(path, file.Name);
        using var outFile = new FileStream(filename, FileMode.Create, FileAccess.Write);

        int remaining = (int)file.Length;
        do
        {
            if (remaining > 0)
            {
                int nread = filestream.Read(buffer, 0, Math.Min(buffer.Length, remaining));
                if (nread == 0) break;
                outFile.Write(buffer, 0, nread);
                remaining -= nread;
            }

            //Debug.WriteLine($"{file}: read {nread} bytes");
        } while (remaining > 0);

        outFile.Close();
        File.SetLastWriteTime(filename, file.WriteDateTime);
    }

    static byte[] readRawImage(string file)
    {
        using var filestream = new FileStream(file, FileMode.Open, FileAccess.Read);
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

    public void OpenImage(string file)
    {
        var image = readRawImage(file);
        FileSystem?.Dispose();

        FileSystem = new(image);
    }

    public FatFile? GetRoot(bool isUTC = false)
    {
        return FileSystem?.GetRoot(isUTC);
    }

    public void Dispose()
    {
        FileSystem?.Dispose();
        GC.SuppressFinalize(this);
    }
}
