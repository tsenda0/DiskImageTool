namespace DiskImageTool;

public class DcuExtractor : IImageExtractor
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

    ImageFile? RootDir = null;

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
    public bool OpenImage(string file)
    {
        using var stream = new FileStream(file, FileMode.Open);
        return OpenImage(stream);
    }

    public bool OpenImage(Stream stream)
    {
        var dcuImage = readDCUImage(stream);
        var newBuf = rebuildDCUImage(dcuImage);

        FileSystem?.Dispose();
        RootDir = null;
        lastUTCflag = false;

        FileSystem = new(newBuf);

        return true;
    }

    bool lastUTCflag = false;

    public ImageFile? GetRoot(bool isUTC)
    {
        if (RootDir == null || lastUTCflag != isUTC)
        {
            RootDir = FileSystem?.GetRoot(isUTC);
            lastUTCflag = isUTC;
        }

        return RootDir;
    }

    /// <summary>
    /// DCUファイルの読み込み
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private static byte[] readDCUImage(Stream filestream)
    {
        var buffer = new byte[BUFSIZE];

        using var workMemStream = new MemoryStream();
        int nread;
        do
        {
            nread = filestream.Read(buffer, 0, buffer.Length);
            if (nread == 0) break;

            workMemStream.Write(buffer, 0, nread);
        } while (nread > 0);

        return workMemStream.ToArray();
    }

    /// <summary>
    /// DCUイメージ内のファイルを抽出
    /// </summary>
    /// <param name="file"></param>
    /// <param name="path"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void ExtractFile(ImageFile file, string path)
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
