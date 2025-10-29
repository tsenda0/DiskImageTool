namespace DiskImageTool;

public class FileSystemFactory : IFileSystemFactory
{
    public IFileSystem Create(IImageReader reader, FileSystemType type)
    {
        return type switch
        {
            FileSystemType.FAT => new FatFileSystem(reader),
            FileSystemType.Unknown => throw new InvalidOperationException("ファイルシステムが不明です"),
            _ => throw new InvalidOperationException("ファイルシステムが不明です"),
        };
    }
}
public interface IFileSystemFactory
{
    IFileSystem Create(IImageReader reader, FileSystemType type);
}

public enum FileSystemType
{
    Unknown = 0,
    FAT = 1,
}
