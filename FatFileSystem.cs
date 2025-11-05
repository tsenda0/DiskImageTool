using System.Buffers.Binary;
using System.ComponentModel;
using System.Text;
using System.IO;

namespace DiskImageTool;

public enum FatType
{
    Unknown = 0,
    FAT12 = 1,
    FAT16 = 2,
    FAT32 = 3,
}

/// <summary>
/// FATファイルシステムの操作。
/// FAT12/16フォーマットのみ対応。
/// 扱えるのはルートディレクトリのファイルのみ。
/// </summary>
public class FatFileSystem : IFileSystem
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
    private const int BpbOffsetOEMName = 3;
    private const int BpbOffsetBytesPerSector = 11;
    private const int BpbOffsetSectorsPerCluster = 13;
    private const int BpbOffsetReservedSectorCount = 14;
    private const int BpbOffsetNumFats = 16;
    private const int BpbOffsetRootEntriesCount = 17;
    private const int BpbOffsetTotalSector16 = 19;
    private const int BpbOffsetFatSize16 = 22;
    private const int BpbOffsetTotalSector32 = 32;
    private const int BpbOffsetVolID = 39;
    private const int BpbOffsetVolLabel = 43;
    private const int BpbOffsetFSType = 54;

    private const int BpbFSTypeLength = 8;
    private const int BpbVolLabelLength = 11;
    private const int BpbOEMNameLength = 8;

    // Directory Entry Offsets
    private const int DirEntryOffsetName = 0;
    private const int DirEntryOffsetAttribute = 11;
    private const int DirEntryOffsetFirstClusterHigh = 20;
    private const int DirEntryOffsetTime = 22;
    private const int DirEntryOffsetDate = 24;
    private const int DirEntryOffsetFirstClusterLow = 26;
    private const int DirEntryOffsetFileSize = 28;

    private const int DirEntryNameLength = 11;
    #endregion

    /// <summary>
    /// 読み込まれたファイルシステムのイメージ
    /// </summary>
    readonly byte[]? buffer;

    /// <summary>
    /// bufferのReadOnlySpan表現
    /// </summary>
    private ReadOnlySpan<byte> bufferSpan => buffer;

    /// <summary>
    /// FATエントリのテーブル
    /// </summary>
    readonly List<uint> fat = [];

    readonly Encoding sjisEncoding = Encoding.GetEncoding("shift_jis");

    FatFileEntry? rootDir;

    public FatType FatType { get; private set; }
    public string OEMName { get; private set; } = "";
    public int BytesPerSector { get; private set; }
    public int SectorsPerCluster { get; private set; }
    public int ReservedSectorCount { get; private set; }
    public int NumFats { get; private set; }
    public int TotalSector16 { get; private set; }
    public uint TotalSector32 { get; private set; }
    public int FatSize16 { get; private set; }
    public int RootEntriesCount { get; private set; }
    public int ClusterSize => BytesPerSector * SectorsPerCluster;
    public int FatSizeBytes => FatSize16 * BytesPerSector;

    public uint VolumeID { get; private set; }
    public string VolumeLabel { get; private set; } = "";
    public string FatFileSystemType { get; private set; } = "";

    #region IFileSystem interface

    [Category("基本")]
    public FileSystemType FileSystemType => FileSystemType.FAT;

    [Category("基本")]
    public int ImageSizeBytes => bufferSpan.Length;

    [Category("基本")]
    public string Id => $"{VolumeID}";

    [Category("基本")]
    public string Name => $"{VolumeLabel}";

    #endregion

    static FatFileSystem()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public FatFileSystem(IImageReader reader)
    {
        buffer = reader.GetBuffer();
        rootDir = null;
        initialize();
    }

    /// <summary>
    /// ルートディレクトリのファイル一覧を取得
    /// </summary>
    /// <returns></returns>
    private List<FatFileEntry> getRootDirEntries(bool isUTC)
    {
        // Root directory
        List<FatFileEntry> entries = [];
        for (int i = 0; i < RootEntriesCount; i++)
        {
            var ent = getFatRootDirEntry(i, isUTC);
            if (ent != null) entries.Add(ent);
        }

        return entries;
    }

    /// <summary>
    /// ルートディレクトリ内のファイル情報を読み込む
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    FatFileEntry? getFatRootDirEntry(int i, bool isUTC = false)
    {
        // FAT32の場合、ルートディレクトリの位置をBPBから読む必要がある。
        // このコードではルートディレクトリがクラスタチェーンにないFAT12/16を主に想定している。
        // この実装はFAT12/16のルートディレクトリに対しては正しい。

        FatFileEntry? ent = null;

        int RootDirOffset = ReservedSectorCount * BytesPerSector + FatSize16 * NumFats * BytesPerSector;

        int entryOffset = RootDirOffset + i * DirEntrySize;
        var entrySpan = bufferSpan.Slice(entryOffset, DirEntrySize);

        // file name
        var nameSpan = entrySpan[DirEntryOffsetName..DirEntryNameLength]; // 0, DirEntryNameLength
        var atr = entrySpan[DirEntryOffsetAttribute];

        if (nameSpan[0] != 0 && nameSpan[0] != DEL_MARK
            && (atr & ATR_DIR) == 0 && (atr & ATR_LFN) != ATR_LFN)
        {
            // ファイル名をSJIS文字として解釈
            var basename = sjisEncoding.GetString(nameSpan[..8]).TrimEnd();
            var ext = sjisEncoding.GetString(nameSpan.Slice(8, 3)).TrimEnd();
            var filename = (ext.Length > 0) ? string.Concat(basename, ".", ext) : basename;

            // write date/time
            ushort time = BinaryPrimitives.ReadUInt16LittleEndian(entrySpan[DirEntryOffsetTime..]);
            ushort date = BinaryPrimitives.ReadUInt16LittleEndian(entrySpan[DirEntryOffsetDate..]);

            DateTime writeDateTime = getFatDateTime(time, date, isUTC);

            // start cluster
            // 上位16bit。FAT32で有効。FAT12/16では使われない
            uint firstH = BinaryPrimitives.ReadUInt16LittleEndian(entrySpan[DirEntryOffsetFirstClusterHigh..]);
            // 下位16bit
            uint firstL = BinaryPrimitives.ReadUInt16LittleEndian(entrySpan[DirEntryOffsetFirstClusterLow..]);
            uint firstCluster = (firstH << 16) | firstL;

            // file size
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(entrySpan[DirEntryOffsetFileSize..]);

            ent = new FatFileEntry(filename, firstCluster, writeDateTime, size);
        }

        return ent;
    }

    #region IFileSystem interface

    /// <summary>
    /// ルートディレクトリエントリ
    /// </summary>
    public IFileEntry GetRoot(bool isUTC)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        List<FatFileEntry> entries = getRootDirEntries(isUTC);
        DateTime dt;
        try
        {
            var rootDate = (ushort)(VolumeID >> 16);
            var rootTime = (ushort)(VolumeID & 0xffff);
            dt = getFatDateTime(rootTime, rootDate, isUTC);
        }
        catch (Exception)
        {
            dt = DateTime.MinValue;
        }
        rootDir = new FatFileEntry("\\", 0, dt, entries);

        return rootDir;
    }

    /// <summary>
    /// ファイルシステム内のファイルを開く
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public Stream OpenFile(IFileEntry file)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        if (bufferSpan.IsEmpty) throw new InvalidOperationException("イメージが開かれていません");

        if (file is not FatFileEntry fatEntry)
            throw new InvalidOperationException("異なるファイルシステムのファイルです");

        // ファイルサイズに基づいてバッファを一度だけ確保する
        byte[] fileBuffer = new byte[fatEntry.Length];
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

        uint cluster = fatEntry.FirstCluster;
        do
        {
            if (cluster == 0) break;
            //Debug.Write($"{cluster} => ");

            int sourceOffset = (int)(datastart + (cluster - 2) * ClusterSize);
            int bytesToCopy = Math.Min(ClusterSize, fileSpan.Length - bytesCopied);
            bufferSpan.Slice(sourceOffset, bytesToCopy).CopyTo(fileSpan[bytesCopied..]);
            bytesCopied += bytesToCopy;

            cluster = fat[(int)cluster];
        } while (cluster is > 0 && cluster < EMark && bytesCopied < fatEntry.Length);

        //Debug.WriteLine($"{(cluster >= EMark ? "END" : cluster)}");

        return new MemoryStream(fileBuffer, 0, bytesCopied, false);
    }

    public Stream OpenFile(string path)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        if (rootDir == null) throw new InvalidOperationException("ルートディレクトリが開かれていません");

        var ent = rootDir.GetFiles().FirstOrDefault(f => string.Equals(f.Name, path, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"ルートディレクトリにファイル '{path}' がありません");

        return OpenFile(ent);
    }

    public async Task ExtractFile(IFileEntry file, string destFullPath, bool isUTC, CancellationToken cancelToken)
    {
        using var stream = OpenFile(file);
        using var outFile = new FileStream(destFullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(outFile, cancelToken);
        outFile.Close();

        if (isUTC)
            File.SetLastWriteTimeUtc(destFullPath, file.WriteDateTime);
        else
            File.SetLastWriteTime(destFullPath, file.WriteDateTime);
    }

    public async Task ExtractFile(string path, string destFullPath, bool isUTC, CancellationToken cancelToken)
    {
        if (rootDir == null) throw new InvalidOperationException("ルートディレクトリが開かれていません");

        var ent = rootDir.GetFiles().FirstOrDefault(f => string.Equals(f.Name, path, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"ファイル '{path}' がありません");

        using var stream = OpenFile(ent);
        using var outFile = new FileStream(destFullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(outFile, cancelToken);
        outFile.Close();

        if (isUTC)
            File.SetLastWriteTimeUtc(destFullPath, ent.WriteDateTime);
        else
            File.SetLastWriteTime(destFullPath, ent.WriteDateTime);
    }

    #endregion

    private void initialize()
    {
        readBPB();
        readFAT(FatType);
    }

    /// <summary>
    /// FATに記録された日時をDateTimeに変換
    /// </summary>
    /// <param name="time"></param>
    /// <param name="date"></param>
    /// <returns></returns>
    private static DateTime getFatDateTime(ushort time, ushort date, bool isUTC = false)
    {
        int year = FAT_EPOCH_YEAR + (date >> 9);
        int month = (date & 0x1e0) >> 5;
        int day = date & 0x1f;
        int hour = time >> 11;
        int minute = (time & 0x7e0) >> 5;
        int second = (time & 0x1f) * 2;
        DateTime writeDateTime = isUTC
            ? new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).ToLocalTime()
            : new DateTime(year, month, day, hour, minute, second);
        return writeDateTime;
    }

    /// <summary>
    /// FATの読み込み
    /// </summary>
    /// <returns></returns>
    private void readFAT(FatType fatType)
    {
        //FATの読み込み
        switch (fatType)
        {
            case FatType.FAT12:
                readFAT12();
                break;
            case FatType.FAT16:
                readFAT16();
                break;
            case FatType.FAT32:
                readFAT32();
                break;
            case FatType.Unknown:
            default:
                throw new ArgumentException("FATタイプが不明です");
        }
    }

    void readFAT12()
    {
        fat.Clear();

        int pos = ReservedSectorCount * BytesPerSector;
        var fatSpan = bufferSpan.Slice(pos, FatSize16 * BytesPerSector); // 1番目のFATのみ読む

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

    void readFAT16()
    {
        fat.Clear();

        int pos = ReservedSectorCount * BytesPerSector;
        var fatSpan = bufferSpan.Slice(pos, FatSize16 * BytesPerSector);

        for (pos = 0; pos < fatSpan.Length; pos += 2)
        {
            //2bytesあたり1つのFATエントリ
            ushort ent = BinaryPrimitives.ReadUInt16LittleEndian(fatSpan.Slice(pos, 2));
            fat.Add(ent);
        }
    }

    void readFAT32()
    {
        fat.Clear();

        int pos = ReservedSectorCount * BytesPerSector;
        var fatSpan = bufferSpan.Slice(pos, FatSize16 * BytesPerSector);

        for (pos = 0; pos < fatSpan.Length; pos += 4)
        {
            //4bytesあたり1つのFATエントリ
            uint ent = BinaryPrimitives.ReadUInt32LittleEndian(fatSpan.Slice(pos, 4));
            fat.Add(ent);
        }
    }

    /// <summary>
    /// FATのBIOS Parameter Blockの読み込み
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void readBPB()
    {
        // Span<T> を使って安全に読み込む
        var bpbSpan = bufferSpan;

        OEMName = Encoding.ASCII.GetString(bpbSpan[BpbOffsetOEMName..(BpbOffsetOEMName + BpbOEMNameLength)]);

        BytesPerSector = BinaryPrimitives.ReadUInt16LittleEndian(bpbSpan[BpbOffsetBytesPerSector..]);
        if (BytesPerSector is not 512 and not 1024 and not 2048 and not 4096)
            throw new InvalidOperationException("セクタサイズが不正です");

        // 13(1) sectors per cluster
        SectorsPerCluster = bpbSpan[BpbOffsetSectorsPerCluster];

        // 14(2) reserved sector count
        ReservedSectorCount = BinaryPrimitives.ReadUInt16LittleEndian(bpbSpan[BpbOffsetReservedSectorCount..]);

        // 16(1) number of FATs
        NumFats = bpbSpan[BpbOffsetNumFats];

        // 17(2) root entries count
        RootEntriesCount = BinaryPrimitives.ReadUInt16LittleEndian(bpbSpan[BpbOffsetRootEntriesCount..]);

        // 19(2) total sectors
        TotalSector16 = BinaryPrimitives.ReadUInt16LittleEndian(bpbSpan[BpbOffsetTotalSector16..]);

        // 22(2) FAT size(number of sector)
        FatSize16 = BinaryPrimitives.ReadUInt16LittleEndian(bpbSpan[BpbOffsetFatSize16..]);

        TotalSector32 = BinaryPrimitives.ReadUInt32LittleEndian(bpbSpan[BpbOffsetTotalSector32..]);

        VolumeID = BinaryPrimitives.ReadUInt32LittleEndian(bpbSpan[BpbOffsetVolID..]);

        VolumeLabel = sjisEncoding.GetString(bpbSpan[BpbOffsetVolLabel..(BpbOffsetVolLabel + BpbVolLabelLength)]).TrimEnd();
        FatFileSystemType = Encoding.ASCII.GetString(bpbSpan[BpbOffsetFSType..(BpbOffsetFSType + BpbFSTypeLength)]);

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

    bool isDisposed;

    protected virtual void Dispose(bool disposing)
    {
        if (isDisposed) return;

        if (disposing)
        {
            // dispose managed resource
            // buffer は GC 対象。fat はクリアする。
            fat.Clear();
            rootDir = null;
        }

        // dispose unmanaged resource

        isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }
}
