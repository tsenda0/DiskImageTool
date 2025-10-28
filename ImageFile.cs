namespace DiskImageTool;

public class ImageFile : IDisposable
{
    readonly IEnumerable<ImageFile>? subEntries;

    public string Name { get; }

    public uint FirstCluster { get; }

    public uint Length { get; }

    public DateTime WriteDateTime { get; }

    public ImageFile(string name, uint firstCluster, uint size, DateTime writeDateTime)
    {
        this.Name = name;
        this.Length = size;
        this.FirstCluster = firstCluster;
        WriteDateTime = writeDateTime;
    }

    public ImageFile(string name, IEnumerable<ImageFile> subEntries)
    {
        this.Name = name;
        this.subEntries = subEntries;
        this.Length = 0;
    }

    public IEnumerable<ImageFile> GetFiles()
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
