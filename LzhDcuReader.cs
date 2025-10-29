using System.Diagnostics;
using SevenZipExtractor;

namespace DiskImageTool;

public class LzhDcuReader : IImageReader
{
    DcuReader? subReader;
    string? tempFile;

    public void Dispose()
    {
        subReader?.Dispose();

        try
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
        catch (Exception)
        {
            Debug.WriteLine($"can not delete {tempFile}");
        }

        GC.SuppressFinalize(this);
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
            return openImageInArchive(ent);
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

        return openImageInArchive(image);
    }

    bool openImageInArchive(Entry entry)
    {
        tempFile = Path.GetTempFileName();
        entry.Extract(tempFile);

        subReader = new DcuReader();
        return subReader.OpenImage(tempFile);
    }

    public bool OpenImage(string file)
    {
        using var stream = new FileStream(file, FileMode.Open);
        return OpenImage(stream);
    }

    public byte[]? GetBuffer()
    {
        return subReader?.GetBuffer();
    }
}
