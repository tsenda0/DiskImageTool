using System.Diagnostics;
using SevenZipExtractor;

namespace DiskImageTool;

public class LzhDcuExtractor : IImageExtractor
{
    DcuExtractor? subExtractor;
    string? TempFile;

    public FatFileSystem? FileSystem => subExtractor?.FileSystem;

    public void Dispose()
    {
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

    public bool OpenImage(Stream stream)
    {
        using var archive = new ArchiveFile(
            stream,
            SevenZipFormat.Lzh);

        var files = archive.Entries;
        var filterdFiles = files
            .Where(f => f.FileName.EndsWith(".DCU", StringComparison.OrdinalIgnoreCase));

        if (files.Count == 0 || !filterdFiles.Any())
        {
            throw new InvalidOperationException("圧縮ファイル内にイメージがありません");
        }

        if (filterdFiles.Count() == 1)
        {
            var ent = filterdFiles.First();
            return OpenImageInArchive(ent);
        }

        using var form = new ArchiveImageSelectForm();
        form.Files = filterdFiles.Select(f => f.FileName);
        DialogResult res = form.ShowDialog();
        if (res == DialogResult.Cancel) return false;
        var selfile = form.SelectedFile;
        if (selfile == null)
        {
            return false;
        }

        var image = filterdFiles.First(f => f.FileName == selfile);

        return OpenImageInArchive(image);
    }

    bool OpenImageInArchive(Entry entry)
    {
        TempFile = Path.GetTempFileName();
        entry.Extract(TempFile);

        subExtractor = new DcuExtractor();
        return subExtractor.OpenImage(TempFile);
    }

    public bool OpenImage(string file)
    {
        using var stream = new FileStream(file, FileMode.Open);
        return OpenImage(stream);
    }
}
