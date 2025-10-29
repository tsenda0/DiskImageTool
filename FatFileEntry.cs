namespace DiskImageTool;

public interface IFileEntry
{
    public IEnumerable<IFileEntry>? SubEntries { get; }

    public string Name { get; }


    public uint Length { get; }

    public DateTime WriteDateTime { get; }

}

public class FatFileEntry : IFileEntry
{
    readonly IEnumerable<FatFileEntry>? subEntries;

    public IEnumerable<IFileEntry>? SubEntries => subEntries;

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
}
