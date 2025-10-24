namespace DiskImageTool;

public class FatFile : IDisposable
{
    readonly IEnumerable<FatFile>? subEntries;

    public string Name { get; }

    public int FirstCluster { get; }

    public uint Length { get; }

    public DateTime WriteDateTime { get; }

    public FatFile(string name, int firstCluster, uint size, DateTime writeDateTime)
    {
        this.Name = name;
        this.Length = size;
        this.FirstCluster = firstCluster;
        WriteDateTime = writeDateTime;
    }

    public FatFile(string name, IEnumerable<FatFile> subEntries)
    {
        this.Name = name;
        this.subEntries = subEntries;
        this.Length = 0;
    }

    public IEnumerable<FatFile> GetFiles()
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
