using System.Diagnostics;

namespace DiskImageTool;

public class RawExtractor : IDisposable, IImageExtractor
{
    /// <summary>
    /// disk buffer
    /// </summary>
    const int BUFSIZE = 1440 * 1024;

    /// <summary>
    /// イメージ内のFATファイルシステム
    /// </summary>
    private FatFileSystem? FileSystem { get; set; }

    public string ImageFile { get; private set; } = "";

    /// <summary>
    /// イメージに含まれるファイルの一覧
    /// </summary>
    private IEnumerable<FatFile> Files { get; set; } = [];


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

            Debug.WriteLine($"{file}: read {nread} bytes");
        } while (nread > 0);

        return workMemStream.ToArray();
    }

    public IEnumerable<FatFile> OpenImage(string file)
    {
        var image = readRawImage(file);
        FileSystem?.Dispose();

        FileSystem = new(image);
        Debug.WriteLine($"FAT type: {FileSystem.FatType}");
        Debug.WriteLine($"bytes per sector: {FileSystem.BytesPerSector}");
        Debug.WriteLine($"sectors per cluster: {FileSystem.SectorsPerCluster}");
        Debug.WriteLine($"reserved sectors count: {FileSystem.ReservedSectorCount}");
        Debug.WriteLine($"total sectors count(16): {FileSystem.TotalSector16}");
        Debug.WriteLine($"total sectors count(32): {FileSystem.TotalSector32}");
        Debug.WriteLine($"number of FATs: {FileSystem.NumFats}");
        Debug.WriteLine($"FAT size(sector count): {FileSystem.FatSize16}");
        Debug.WriteLine($"root entries count: {FileSystem.RootEntriesCount}");

        Debug.WriteLine($"image size: {image.Length}");

        var root = FileSystem.Root;
        this.Files = root.GetFiles();
        this.ImageFile = file;

        return Files;
    }

    public void Dispose()
    {
        FileSystem?.Dispose();
        GC.SuppressFinalize(this);
    }
}
