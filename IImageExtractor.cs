
namespace DiskImageTool
{
    public interface IImageExtractor
    {
        string ImageFile { get; }

        FatFileSystem? FileSystem { get; }

        void ExtractFile(FatFile file, string path);

        IEnumerable<FatFile> OpenImage(string file);
    }
}