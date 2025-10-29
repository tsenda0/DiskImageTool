using System.Text;

namespace DiskImageTool;

/// <summary>
/// FATファイルシステムの操作。
/// FAT12/16フォーマットのみ対応。
/// 扱えるのはルートディレクトリのファイルのみ。
/// </summary>
public class FatFileSystem : IDisposable
{
    /// <summary>
    /// FATファイル日時の開始年
    /// </summary>
    const int FAT_EPOCH_YEAR = 1980;
    /// <summary>
    /// 1ディレクトリエントリの大きさ
    /// </summary>
    const int DirEntrySize = 32;
    /// <summary>
    /// FATの終端値
    /// </summary>
    const uint EMARK_FAT12 = 0xff8;
    const uint EMARK_FAT16 = 0xfff8;
    const uint EMARK_FAT32 = 0xfffffff8;

    const int DEL_MARK = 0xe5;
    const int ATR_LFN = 0x0f;
    const int ATR_DIR = 0x10;

    #region BPB/DirEntry Offsets
    // BPB (BIOS Parameter Block) Offsets
    private const int BpbOffsetBytesPerSector = 11;
    private const int BpbOffsetSectorsPerCluster = 13;
    private const int BpbOffsetReservedSectorCount = 14;
    private const int BpbOffsetNumFats = 16;
    private const int BpbOffsetRootEntriesCount = 17;
    private const int BpbOffsetTotalSector16 = 19;
    private const int BpbOffsetFatSize16 = 22;
    private const int BpbOffsetTotalSector32 = 32;

    // Directory Entry Offsets
    private const int DirEntryOffsetName = 0;
    private const int DirEntryOffsetAttribute = 11;
    private const int DirEntryOffsetFirstClusterHigh = 20;
    private const int DirEntryOffsetTime = 22;
    private const int DirEntryOffsetDate = 24;
    private const int DirEntryOffsetFirstClusterLow = 26;
    private const int DirEntryOffsetFileSize = 28;
    #endregion
    private const int DirEntryNameLength = 11;

    /// <summary>
    /// 読み込まれたファイルシステムのイメージ
    /// </summary>
    readonly byte[] buffer;

    /// <summary>
    /// bufferのReadOnlySpan表現
    /// </summary>
    private ReadOnlySpan<byte> BufferSpan => buffer;

    /// <summary>
    /// FATエントリのテーブル
    /// </summary>
    readonly List<uint> fat = [];

    public FatType FatType { get; private set; }
    public int BytesPerSector { get; private set; }
    public int SectorsPerCluster { get; private set; }
    public int ReservedSectorCount { get; private set; }
    public int NumFats { get; private set; }
    public int TotalSector16 { get; private set; }
    public uint TotalSector32 { get; private set; }
    public int FatSize16 { get; private set; }
    public int RootEntriesCount { get; private set; }
    public int ImageSizeBytes => buffer.Length;
    public int ClusterSize => BytesPerSector * SectorsPerCluster;
    public int FatSizeBytes => FatSize16 * BytesPerSector;

    readonly Encoding SjisEncoding = Encoding.GetEncoding("shift_jis");

    ImageFile? rootDir;

    /// <summary>
    /// ルートディレクトリエントリ
    /// </summary>
    public ImageFile GetRoot(bool isUTC = false)
    {
        List<ImageFile> entries = ReadRootDir(isUTC);
        rootDir = new ImageFile("\\", entries);
        return rootDir;
    }

    static FatFileSystem()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// FATイメージを操作する
    /// </summary>
    /// <param name="image"></param>
    public FatFileSystem(byte[] image)
    {
        buffer = [.. image];
        Initialize();
    }

    /// <summary>
    /// FATイメージを操作する
    /// </summary>
    /// <param name="stream"></param>
    public FatFileSystem(Stream stream)
    {
        buffer = ReadImage(stream);
        Initialize();
    }

    private void Initialize()
    {
        ReadBPB();
        ReadFAT(FatType);
    }

    /// <summary>
    /// ルートディレクトリのファイル一覧を取得
    /// </summary>
    /// <returns></returns>
    private List<ImageFile> ReadRootDir(bool isUTC = false)
    {
        // Root directory
        List<ImageFile> entries = [];
        for (int i = 0; i < RootEntriesCount; i++)
        {
            var ent = GetFatDirEntry(i, isUTC);
            if (ent != null) entries.Add(ent);
        }

        return entries;
    }

    /// <summary>
    /// FATに記録された日時をDateTimeに変換
    /// </summary>
    /// <param name="time"></param>
    /// <param name="date"></param>
    /// <returns></returns>
    private static DateTime GetFatDateTime(ushort time, ushort date, bool isUTC = false)
    {
        int year = FAT_EPOCH_YEAR + (date >> 9);
        int month = (date & 0x1e0) >> 5;
        int day = date & 0x1f;
        int hour = time >> 11;
        int minute = (time & 0x7e0) >> 5;
        int second = time & 0x1f;
        DateTime writeDateTime = isUTC
            ? new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).ToLocalTime()
            : new DateTime(year, month, day, hour, minute, second);
        return writeDateTime;
    }

    /// <summary>
    /// ルートディレクトリ内のファイル情報を読み込む
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    ImageFile? GetFatDirEntry(int i, bool isUTC = false)
    {
        // FAT32の場合、ルートディレクトリの位置をBPBから読む必要がある。
        // このコードではルートディレクトリがクラスタチェーンにないFAT12/16を主に想定している。
        // この実装はFAT12/16のルートディレクトリに対しては正しい。

        ImageFile? ent = null;

        int RootDirOffset = ReservedSectorCount * BytesPerSector + FatSize16 * NumFats * BytesPerSector;

        int entryOffset = RootDirOffset + i * DirEntrySize;
        var entrySpan = BufferSpan.Slice(entryOffset, DirEntrySize);

        // file name
        var nameSpan = entrySpan[DirEntryOffsetName..DirEntryNameLength]; // 0, DirEntryNameLength
        var atr = entrySpan[DirEntryOffsetAttribute];

        if (nameSpan[0] != 0 && nameSpan[0] != DEL_MARK
            && ((atr & ATR_DIR) == 0) && ((atr & ATR_LFN) != ATR_LFN))
        {
            // ファイル名をSJIS文字として解釈
            var basename = SjisEncoding.GetString(nameSpan[..8]).TrimEnd();
            var ext = SjisEncoding.GetString(nameSpan.Slice(8, 3)).TrimEnd();
            var filename = (ext.Length > 0) ? string.Concat(basename, ".", ext) : basename;

            // write date/time
            ushort time = BitConverter.ToUInt16(entrySpan[DirEntryOffsetTime..]);
            ushort date = BitConverter.ToUInt16(entrySpan[DirEntryOffsetDate..]);

            DateTime writeDateTime = GetFatDateTime(time, date, isUTC);

            // start cluster
            // 上位16bit。FAT32で有効。FAT12/16では使われない
            uint firstH = BitConverter.ToUInt16(entrySpan[DirEntryOffsetFirstClusterHigh..]);
            // 下位16bit
            uint firstL = BitConverter.ToUInt16(entrySpan[DirEntryOffsetFirstClusterLow..]);
            uint firstCluster = (firstH << 16) | firstL;

            // file size
            uint size = BitConverter.ToUInt32(entrySpan[DirEntryOffsetFileSize..]);

            ent = new ImageFile(filename, firstCluster, size, writeDateTime);
        }

        return ent;
    }

    /// <summary>
    /// FATの読み込み
    /// </summary>
    /// <returns></returns>
    private void ReadFAT(FatType fatType)
    {
        //FATの読み込み
        switch (fatType)
        {
            case FatType.FAT12:
                ReadFAT12();
                break;
            case FatType.FAT16:
                ReadFAT16();
                break;
            case FatType.FAT32:
                ReadFAT32();
                break;
        }
    }

    void ReadFAT12()
    {
        fat.Clear();

        int pos = ReservedSectorCount * BytesPerSector;
        var fatSpan = BufferSpan.Slice(pos, FatSize16 * BytesPerSector); // 1番目のFATのみ読む

        pos = 0;
        do
        {
            //3bytesあたり2つのFATエントリ
            ushort ent;
            ent = (ushort)(fatSpan[pos]
                | (fatSpan[pos + 1] & 0xf) << 8);
            fat.Add(ent);

            ent = (ushort)((fatSpan[pos + 1] >> 4)
                | (fatSpan[pos + 2] << 4));
            fat.Add(ent);

            pos += 3;
        } while (pos < fatSpan.Length - 2); // 3バイトずつ読むので境界チェックを調整
    }

    void ReadFAT16()
    {
        fat.Clear();

        int pos = ReservedSectorCount * BytesPerSector;
        var fatSpan = BufferSpan.Slice(pos, FatSize16 * BytesPerSector);

        for (pos = 0; pos < fatSpan.Length; pos += 2)
        {
            //2bytesあたり1つのFATエントリ
            ushort ent = BitConverter.ToUInt16(fatSpan.Slice(pos, 2));
            fat.Add(ent);
        }
    }

    void ReadFAT32()
    {
        fat.Clear();

        int pos = ReservedSectorCount * BytesPerSector;
        var fatSpan = BufferSpan.Slice(pos, FatSize16 * BytesPerSector);

        for (pos = 0; pos < fatSpan.Length; pos += 4)
        {
            //4bytesあたり1つのFATエントリ
            uint ent = BitConverter.ToUInt32(fatSpan.Slice(pos, 4));
            fat.Add(ent);
        }
    }

    /// <summary>
    /// ファイルシステムイメージの読み込み
    /// </summary>
    /// <param name="stream"></param>
    /// <returns>読み込んだイメージのバイト配列</returns>
    private static byte[] ReadImage(Stream stream)
    {
        if (stream is MemoryStream ms)
        {
            return ms.ToArray();
        }

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// FATのBIOS Parameter Blockの読み込み
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void ReadBPB()
    {
        // Span<T> を使って安全に読み込む
        var bpbSpan = BufferSpan;

        BytesPerSector = BitConverter.ToUInt16(bpbSpan[BpbOffsetBytesPerSector..]);
        if (BytesPerSector is not 512 and not 1024 and not 2048 and not 4096)
            throw new InvalidOperationException("セクタサイズが不正です");

        // 13(1) sectors per cluster
        SectorsPerCluster = bpbSpan[BpbOffsetSectorsPerCluster];

        // 14(2) reserved sector count
        ReservedSectorCount = BitConverter.ToUInt16(bpbSpan[BpbOffsetReservedSectorCount..]);

        // 16(1) number of FATs
        NumFats = bpbSpan[BpbOffsetNumFats];

        // 17(2) root entries count
        RootEntriesCount = BitConverter.ToUInt16(bpbSpan[BpbOffsetRootEntriesCount..]);

        // 19(2) total sectors
        TotalSector16 = BitConverter.ToUInt16(bpbSpan[BpbOffsetTotalSector16..]);

        // 22(2) FAT size(number of sector)
        FatSize16 = BitConverter.ToUInt16(bpbSpan[BpbOffsetFatSize16..]);

        TotalSector32 = BitConverter.ToUInt32(bpbSpan[BpbOffsetTotalSector32..]);

        // FAT種別の推定
        int rootDirSector = ReservedSectorCount + FatSize16 * NumFats;
        int dataStartSector = rootDirSector + (DirEntrySize * RootEntriesCount + BytesPerSector - 1) / BytesPerSector;

        uint numClusters = Convert.ToUInt32(((TotalSector16 == 0 ? TotalSector32 : TotalSector16) - dataStartSector) / SectorsPerCluster);
        FatType = numClusters <= 4085
            ? FatType.FAT12
            : numClusters <= 65525
                ? FatType.FAT16
                : FatType.FAT32;
    }

    /// <summary>
    /// ファイルシステム内のファイルを開く
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public Stream OpenFile(ImageFile file)
    {
        // ファイルサイズに基づいてバッファを一度だけ確保する
        byte[] fileBuffer = new byte[file.Length];
        var fileSpan = fileBuffer.AsSpan();
        int bytesCopied = 0;

        //Debug.Write($"{file.Name}: {file.Length} bytes: scanning cluster: ");

        int datastart = (ReservedSectorCount + FatSize16 * NumFats) * BytesPerSector;
        datastart += DirEntrySize * RootEntriesCount;

        var EMark = FatType switch
        {
            FatType.FAT12 => EMARK_FAT12,
            FatType.FAT16 => EMARK_FAT16,
            FatType.FAT32 => EMARK_FAT32,
            FatType.Unknown => throw new InvalidOperationException("FAT種別が不明です"),
            _ => throw new InvalidOperationException("FAT種別が不明です"),
        };

        uint cluster = file.FirstCluster;
        do
        {
            if (cluster == 0) break;
            //Debug.Write($"{cluster} => ");

            int sourceOffset = (int)(datastart + (cluster - 2) * ClusterSize);
            int bytesToCopy = Math.Min(ClusterSize, fileSpan.Length - bytesCopied);
            BufferSpan.Slice(sourceOffset, bytesToCopy).CopyTo(fileSpan[bytesCopied..]);
            bytesCopied += bytesToCopy;

            cluster = fat[(int)cluster];
        } while (cluster is > 0 && cluster < EMark && bytesCopied < file.Length);

        //Debug.WriteLine($"{(cluster >= EMark ? "END" : cluster)}");

        return new MemoryStream(fileBuffer, 0, bytesCopied, false);
    }

    /// <summary>
    /// ファイルシステム内のファイルを開く
    /// </summary>
    /// <param name="file"></param>D
    /// <returns></returns>
    public Stream OpenFile(string path)
    {
        if (buffer.Length == 0 || rootDir == null) throw new InvalidOperationException("FATイメージが開かれていません");

        var ent = rootDir.GetFiles().FirstOrDefault(f => f.Name == path)
            ?? throw new InvalidOperationException($"ファイル '{path}' がありません");
        return OpenFile(ent);
    }


    public void Dispose()
    {
        //Root?.Dispose();
        // buffer は GC 対象。fat はクリアする。
        fat.Clear(); // List<T>のリソースを解放

        GC.SuppressFinalize(this);
    }
}
