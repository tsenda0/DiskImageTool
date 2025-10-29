namespace DiskImageTool;

public class FatFileEntry : IDisposable
{
    readonly IEnumerable<FatFileEntry>? subEntries;

    public string Name { get; }

    public uint FirstCluster { get; }

    public uint Length { get; }

    public DateTime WriteDateTime { get; }

    public FatFileEntry(string name, uint firstCluster, uint size, DateTime writeDateTime)
    {
        Name = name;
        Length = size;
        FirstCluster = firstCluster;
        WriteDateTime = writeDateTime;
    }

    public FatFileEntry(string name, IEnumerable<FatFileEntry> subEntries)
    {
        Name = name;
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

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
