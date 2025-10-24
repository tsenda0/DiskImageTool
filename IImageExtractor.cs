
namespace DiskImageTool
{
    public interface IImageExtractor
    {
        string ImageFile { get; }

        void ExtractFile(FatFile file, string path);
        IEnumerable<FatFile> OpenImage(string file);
    }
}