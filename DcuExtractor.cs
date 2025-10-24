using System.Diagnostics;

namespace DiskImageTool;

public class DcuExtractor : IDisposable, IImageExtractor
{
    /// <summary>
    /// disk buffer
    /// </summary>
    const int BUFSIZE = 1440 * 1024;
    /// <summary>
    /// DCU header size
    /// </summary>
    const int DcuOffset = 0xa2;
    /// <summary>
    /// 1.25FD number of cylinders
    /// </summary>
    const int CYLINDERS = 77;
    /// <summary>
    /// 1.25FD number of heads
    /// </summary>
    const int HEADS = 2;
    /// <summary>
    /// 1.25FD sectors per track
    /// </summary>
    const int SECTOR_PER_TRACK = 8;
    /// <summary>
    /// 1.25FD bytes per sector
    /// </summary>
    const int BYTES_PER_SECTOR = 1024;
    /// <summary>
    /// DCU track map size
    /// </summary>
    const int TRACKMAP_SIZE = 160;

    /// <summary>
    /// イメージ内のFATファイルシステム
    /// </summary>
    public FatFileSystem? FileSystem { get; private set; }

    /// <summary>
    /// イメージに含まれるファイルの一覧
    /// </summary>
    private IEnumerable<FatFile> Files { get; set; } = [];

    /// <summary>
    /// DCUイメージファイル
    /// </summary>
    public string ImageFile { get; private set; } = "";

    /// <summary>
    /// トラックマップを使用してイメージを再構築(未使用トラックの情報を反映する)
    /// </summary>
    /// <param name="dcuImage"></param>
    /// <returns></returns>
    static byte[] rebuildDCUImage(byte[] dcuImage)
    {
        // rebuild image
        var trackMap = dcuImage.AsSpan(1, TRACKMAP_SIZE); //srcBuf.GetRange(1, TRACKMAP_SIZE);
        var newBuf = new byte[CYLINDERS * HEADS * SECTOR_PER_TRACK * BYTES_PER_SECTOR];

        // 未使用トラックを考慮しデータを再配置
        int track = 0;
        int srcPos = DcuOffset;
        int destPos = 0;
        int trackSize = SECTOR_PER_TRACK * BYTES_PER_SECTOR;
        for (int cylinder = 0; cylinder < CYLINDERS; cylinder++)
        {
            for (int head = 0; head < HEADS; head++)
            {
                if (trackMap[track] == 1)
                {
                    Buffer.BlockCopy(dcuImage, srcPos, newBuf, destPos, trackSize);

                    srcPos += trackSize;
                }
                destPos += trackSize;

                //Debug.WriteLine($"CHS={cylinder}/{head} => track={track},srcPos={srcPos}, destPos={destPos}, logsec={logsec}");

                track++;
            }
        }

        return newBuf;
    }

    /// <summary>
    /// DCUイメージファイルを読み込み、内部のFATファイルシステムを解析してルートディレクトリのファイル一覧を取得
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public IEnumerable<FatFile> OpenImage(string file)
    {
        var dcuImage = readDCUImage(file);
        var newBuf = rebuildDCUImage(dcuImage);

        FileSystem?.Dispose();

        FileSystem = new(newBuf);

        if (FileSystem.Root != null)
        {
            ImageFile = file;
            Files = FileSystem.Root.GetFiles();
        }
        else
        {
            Files = [];
        }

        return Files;
    }

    /// <summary>
    /// DCUファイルの読み込み
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private static byte[] readDCUImage(string file)
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

    /// <summary>
    /// DCUイメージ内のファイルを抽出
    /// </summary>
    /// <param name="file"></param>
    /// <param name="path"></param>
    /// <exception cref="InvalidOperationException"></exception>
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

    public void Dispose()
    {
        FileSystem?.Dispose();
        GC.SuppressFinalize(this);
    }
}
