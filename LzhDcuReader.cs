using System.Diagnostics;
using SevenZipExtractor;
using System.IO;

namespace DiskImageTool;

public interface IFileSelectDialog
{
    bool ShowDialog(IEnumerable<string> files);
    string? SelectedFile { get; }
}

public class LzhDcuReader(IFileSelectDialog dialog) : IImageReader
{
    DcuReader? subReader;
    string? tempFile;

    string baseFileName = "";
    string imageFileName = "";

    readonly IFileSelectDialog dialog = dialog;

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

        /*
        var form = new ArchiveImageSelectForm
        {
            Files = filterdFiles.Select(f => f.FileName)
        };
        form.ShowDialog();
        */
        var res = dialog.ShowDialog(filterdFiles.Select(f => f.FileName));
        if (!res) return false;
        var selfile = dialog.SelectedFile;
        if (selfile == null)
        {
            return false;
        }

        var image = filterdFiles.First(f => string.Equals(f.FileName, selfile, StringComparison.OrdinalIgnoreCase));
        baseFileName = "(stream)";

        return openImageInArchive(image);
    }

    public bool OpenImage(string file)
    {
        using var stream = new FileStream(file, FileMode.Open);
        var ret = OpenImage(stream);
        baseFileName = Path.GetFileName(file);
        return ret;
    }

    bool openImageInArchive(Entry entry)
    {
        tempFile = Path.GetTempFileName();
        entry.Extract(tempFile);

        imageFileName = entry.FileName;
        subReader = new DcuReader();
        return subReader.OpenImage(tempFile);
    }

    public string OpenFileName => $"{baseFileName} / {imageFileName}";

    public byte[]? GetBuffer()
    {
        return subReader?.GetBuffer();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    bool isDisposed;

    protected virtual void Dispose(bool disposing)
    {
        if (isDisposed) return;

        if (disposing)
        {
            // dispose managed resource
            subReader?.Dispose();
        }

        // dispose unmanaged resource
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

        tempFile = null;
        isDisposed = true;
    }
}
