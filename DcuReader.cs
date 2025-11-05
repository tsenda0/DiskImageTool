
using System.IO;

namespace DiskImageTool;
public class DcuReader : IImageReader
{
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

    byte[]? image;

    /// <summary>
    /// DCUファイルの読み込み
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private static byte[] readDCUImage(Stream filestream)
    {
        using var workMemStream = new MemoryStream();
        filestream.CopyTo(workMemStream);
        return workMemStream.ToArray();
    }

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
        var ret = OpenImage(stream);
        OpenFileName = Path.GetFileName(file);
        return ret;
    }

    public bool OpenImage(Stream stream)
    {
        var dcuImage = readDCUImage(stream);
        image = rebuildDCUImage(dcuImage);
        OpenFileName = "(stream)";
        return true;
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
