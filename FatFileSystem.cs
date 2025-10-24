using System.Diagnostics;

namespace DiskImageTool;

/// <summary>
/// FATファイルシステムの操作
/// </summary>
public class FatFileSystem : IDisposable
{
    /// <summary>
    /// disk buffer
    /// </summary>
    const int BUFSIZE = 1440 * 1024;
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

    /// <summary>
    /// 読み込まれたファイルシステムのイメージ
    /// </summary>
    readonly List<byte> buffer = [];
    /// <summary>
    /// FATエントリのテーブル
    /// </summary>
    readonly List<uint> fat = [];
    /// <summary>
    /// ルートディレクトリのファイル一覧
    /// </summary>
    readonly FatFile? RootDirEntry;

    public FatType FatType { get; private set; }
    public int BytesPerSector { get; private set; }
    public int SectorsPerCluster { get; private set; }
    public int ReservedSectorCount { get; private set; }
    public int NumFats { get; private set; }
    public int TotalSector16 { get; private set; }
    public uint TotalSector32 { get; private set; }
    public int FatSize16 { get; private set; }
    public int RootEntriesCount { get; private set; }

    /// <summary>
    /// FATイメージを操作する
    /// </summary>
    /// <param name="image"></param>
    public FatFileSystem(byte[] image)
    {
        using var stream = new MemoryStream(image);

        ReadImage(stream);
        ReadBPB();
        ReadFAT(FatType);

        List<FatFile> entries = readRootDir();
        RootDirEntry = new FatFile("/", entries);
    }

    /// <summary>
    /// FATイメージを操作する
    /// </summary>
    /// <param name="stream"></param>
    public FatFileSystem(Stream stream)
    {
        ReadImage(stream);
        ReadBPB();
        ReadFAT(FatType);

        List<FatFile> entries = readRootDir();
        RootDirEntry = new FatFile("\\", entries);
    }

    /// <summary>
    /// ルートディレクトリのファイル一覧を取得
    /// </summary>
    /// <returns></returns>
    private List<FatFile> readRootDir()
    {
        // Root directory
        List<FatFile> entries = [];
        for (int i = 0; i < RootEntriesCount; i++)
        {
            var ent = GetFatDirEntry(i);
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
    private static DateTime GetFatDateTime(ushort time, ushort date)
    {
        int year = FAT_EPOCH_YEAR + (date >> 9);
        int month = (date & 0x1e0) >> 5;
        int day = date & 0x1f;
        int hour = time >> 11;
        int minute = (time & 0x7e0) >> 5;
        int second = time & 0x1f;
        DateTime writeDateTime = new DateTime(year, month, day, hour, minute, second);
        return writeDateTime;
    }

    /// <summary>
    /// ルートディレクトリ内のファイル情報を読み込む
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    FatFile? GetFatDirEntry(int i)
    {
        FatFile? ent = null;

        int RootDirOffset = ReservedSectorCount * BytesPerSector + FatSize16 * NumFats * BytesPerSector;

        // file name
        char[] entchars = new char[11];
        for (int c = 0; c < 11; c++)
        {
            entchars[c] = Convert.ToChar(buffer[RootDirOffset + i * DirEntrySize + c]);
        }
        var atr = buffer[RootDirOffset + i * DirEntrySize + 11];

        if (entchars[0] != 0 && entchars[0] != DEL_MARK
            && ((atr & ATR_DIR) == 0) && ((atr & ATR_LFN) != ATR_LFN))
        {
            string strname = new(entchars);
            var basename = strname.AsSpan(0, 8).Trim();
            var ext = strname.AsSpan(8, 3).Trim();
            var filename = (ext.Length > 0) ? string.Concat(basename, ".", ext) : basename;

            // write date/time
            int pos = RootDirOffset + i * DirEntrySize + 22;
            ushort time = buffer.GetUshort(pos);

            pos = RootDirOffset + i * DirEntrySize + 24;
            ushort date = buffer.GetUshort(pos);

            DateTime writeDateTime = GetFatDateTime(time, date);

            // start cluster
            pos = RootDirOffset + i * DirEntrySize + 20;
            uint firstH = buffer.GetUshort(pos);
            pos = RootDirOffset + i * DirEntrySize + 26;
            uint firstL = buffer.GetUshort(pos);

            // file size
            pos = RootDirOffset + i * DirEntrySize + 28;
            uint size = buffer.GetUint(pos);

            ent = new FatFile(filename.ToString(), (firstH << 16) | firstL, size, writeDateTime);
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
        List<byte> fatTmp = buffer.GetRange(pos, FatSize16 * NumFats * BytesPerSector);

        pos = 0;
        do
        {
            //3bytesあたり2つのFATエントリ
            ushort ent;
            ent = (ushort)(fatTmp[pos]
                | (fatTmp[pos + 1] & 0xf) << 8);
            fat.Add(ent);

            ent = (ushort)((fatTmp[pos + 1] >> 4)
                | (fatTmp[pos + 2] << 4));
            fat.Add(ent);

            pos += 3;
        } while (pos < (fatTmp.Count / NumFats) - 3);
    }

    void ReadFAT16()
    {
        fat.Clear();

        int pos = ReservedSectorCount * BytesPerSector;
        List<byte> fatTmp = buffer.GetRange(pos, FatSize16 * NumFats * BytesPerSector);

        pos = 0;
        do
        {
            //2bytesあたり1つのFATエントリ
            ushort ent;
            ent = fatTmp.GetUshort(pos);
            pos += 2;
            fat.Add(ent);
        } while (pos < (fatTmp.Count / NumFats));
    }

    void ReadFAT32()
    {
        fat.Clear();

        int pos = ReservedSectorCount * BytesPerSector;
        List<byte> fatTmp = buffer.GetRange(pos, FatSize16 * NumFats * BytesPerSector);

        pos = 0;
        do
        {
            //4bytesあたり1つのFATエントリ
            uint ent;
            ent = fatTmp.GetUint(pos);
            fat.Add(ent);
            pos += 4;
        } while (pos < (fatTmp.Count / NumFats));
    }

    /// <summary>
    /// ファイルシステムイメージの読み込み
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    int ReadImage(Stream stream)
    {
        buffer.Clear();
        byte[] readBuffer = new byte[BUFSIZE];
        int nread;
        do
        {
            nread = stream.Read(readBuffer, 0, readBuffer.Length);
            if (nread == 0) break;
            buffer.AddRange(readBuffer);
        } while (nread > 0);

        return buffer.Count;
    }

    /// <summary>
    /// FATのBIOS Parameter Blockの読み込み
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void ReadBPB()
    {
        int pos;
        //int pos = 0;
        //if (buffer[pos++] != 0xeb) throw new InvalidOperationException("not a FAT image");
        //pos++;
        //if (buffer[pos++] != 0x90) throw new InvalidOperationException("not a FAT image");

        // 11(2) reserved sectors
        pos = 11;
        BytesPerSector = buffer.GetUshort(pos++); //buffer[pos++] | buffer[pos++] << 8;
        if (BytesPerSector is not 512 and not 1024 and not 2048 and not 4096)
            throw new InvalidOperationException("セクタサイズが不正です");
        pos++;

        // 13(1) sectors per cluster
        SectorsPerCluster = buffer[pos++]; //buffer[pos++];

        // 14(2) reserved sector count
        ReservedSectorCount = buffer.GetUshort(pos++); //buffer[pos++] | buffer[pos++] << 8;
        pos++;

        // 16(1) number of FATs
        NumFats = buffer[pos++];

        // 17(2) root entries count
        RootEntriesCount = buffer.GetUshort(pos++); //buffer[pos++] | buffer[pos++] << 8;
        pos++;

        // 19(2) total sectors
        TotalSector16 = buffer.GetUshort(pos++); // buffer[pos++] | buffer[pos++] << 8;
        pos += 2;

        // 22(2) FAT size(number of sector)
        FatSize16 = buffer.GetUshort(pos); // buffer[pos++] | buffer[pos++] << 8;

        pos = 32;
        TotalSector32 = buffer.GetUint(pos);

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
    public Stream OpenFile(FatFile file)
    {
        List<byte> fileBuffer = [];
        Debug.Write($"{file.Name}: {file.Length} bytes: scanning cluster: ");

        int datastart = (ReservedSectorCount + FatSize16 * NumFats) * BytesPerSector;
        datastart += DirEntrySize * RootEntriesCount;

        var EMark = FatType == FatType.FAT12 ? EMARK_FAT12 : FatType == FatType.FAT16 ? EMARK_FAT16 : EMARK_FAT32;

        uint cluster = file.FirstCluster;
        do
        {
            if (cluster == 0) break;
            Debug.Write($"{cluster} => ");
            fileBuffer.AddRange(
                buffer.GetRange(
                    (int)(datastart + (cluster - 2) * BytesPerSector * SectorsPerCluster),
                    BytesPerSector * SectorsPerCluster));
            cluster = fat[(int)cluster];
        } while (cluster is > 0 && cluster < EMark);

        Debug.WriteLine($"{(cluster >= EMark ? "END" : cluster)}");

        if (fileBuffer.Count > file.Length) fileBuffer.RemoveRange((int)file.Length, (int)(fileBuffer.Count - file.Length));

        return new MemoryStream([.. fileBuffer]);
    }

    /// <summary>
    /// ファイルシステム内のファイルを開く
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public Stream OpenFile(string path)
    {
        if (buffer.Count == 0 || RootDirEntry == null) throw new InvalidOperationException("FATイメージが開かれていません");

        var ent = RootDirEntry.GetFiles().FirstOrDefault(f => f.Name == path)
            ?? throw new InvalidOperationException($"ファイル '{path}' がありません");
        return OpenFile(ent);
    }

    /// <summary>
    /// ルートディレクトリエントリ
    /// </summary>
    public FatFile Root =>
        buffer.Count == 0 || RootDirEntry == null
            ? throw new InvalidOperationException("FATイメージが開かれていません")
            : RootDirEntry;

    public void Dispose()
    {
        RootDirEntry?.Dispose();
        buffer.Clear();
        fat.Clear();

        GC.SuppressFinalize(this);
    }
}
