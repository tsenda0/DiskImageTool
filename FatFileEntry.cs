namespace DiskImageTool;

public class FatFileEntry : IFileEntry
{
    readonly IEnumerable<FatFileEntry>? subEntries;

    public IEnumerable<IFileEntry>? SubEntries => subEntries;

    public string Name { get; }

    public uint FirstCluster { get; }

    public uint Length { get; }

    public DateTime WriteDateTime { get; }

    public FatFileEntry(string name, uint firstCluster, DateTime writeDateTime, uint size)
    {
        Name = name;
        Length = size;
        FirstCluster = firstCluster;
        WriteDateTime = writeDateTime;
    }

    public FatFileEntry(string name, uint firstCluster, DateTime writeDateTime, IEnumerable<FatFileEntry> subEntries)
    {
        Name = name;
        FirstCluster = firstCluster;
        WriteDateTime = writeDateTime;
        this.subEntries = subEntries;
        Length = 0;
    }

    public IEnumerable<FatFileEntry> GetFiles()
    {
        return subEntries ?? throw new InvalidOperationException("not a directory");
    }

    public override string ToString()
    {
        return $"{Name}";
    }
}
