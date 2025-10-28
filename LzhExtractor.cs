using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SevenZipExtractor;

namespace DiskImageTool;

public class LzhExtractor : IImageExtractor
{
    IImageExtractor? subExtractor;

    public FatFileSystem? FileSystem { get => subExtractor?.FileSystem; }

    public void Dispose()
    {
        //FileSystem?.Dispose();
        subExtractor?.Dispose();

        try
        {
            if (File.Exists(TempFile))
            {
                File.Delete(TempFile);
            }
        }
        catch (Exception)
        {
            Debug.WriteLine($"can not delete {TempFile}");
        }

        GC.SuppressFinalize(this);
    }

    public void ExtractFile(ImageFile file, string path)
    {
        subExtractor?.ExtractFile(file, path);
    }

    public ImageFile? GetRoot(bool isUTC)
    {
        return subExtractor?.GetRoot(isUTC);
    }

    string? TempFile;

    public void OpenImage(Stream stream)
    {
        using SevenZipExtractor.ArchiveFile archive = new SevenZipExtractor.ArchiveFile(
            stream,
            SevenZipFormat.Lzh);

        var files = archive.Entries;
        var filterdFiles = files
            .Where(f => f.FileName.EndsWith(".DCU", StringComparison.OrdinalIgnoreCase));
        if (filterdFiles.Count() == 1)
        {
            var ent = filterdFiles.First();
            OpenImageInArchive(ent);
            return;
        }

        throw new InvalidOperationException("no image or multiple images");
    }

    void OpenImageInArchive(Entry entry)
    {
        TempFile = Path.GetTempFileName();
        entry.Extract(TempFile);

        subExtractor = new DcuExtractor();
        subExtractor.OpenImage(TempFile);
    }

    public void OpenImage(string file)
    {
        using FileStream stream = new FileStream(file, FileMode.Open);
        OpenImage(stream);
    }
}
