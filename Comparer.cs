using System.IO;

namespace DiskImageTool;
sealed class FileNameComparer : IComparer<IFileEntry?>
{
    public int Compare(IFileEntry? x, IFileEntry? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        string base_x = Path.GetFileNameWithoutExtension(x.Name);
        string base_y = Path.GetFileNameWithoutExtension(y.Name);

        return int.TryParse(base_x, out int ix) && int.TryParse(base_y, out int iy)
            ? ix == iy ? 0 : ix > iy ? 1 : -1
            : string.CompareOrdinal(base_x, base_y);
    }
}

sealed class FileSizeComparer : IComparer<IFileEntry?>
{
    public int Compare(IFileEntry? x, IFileEntry? y)
    {
        return x == null && y == null
                    ? 0 : x == null
                        ? -1 : y == null
                            ? 1 : x.Length > y.Length
                                ? 1 : x.Length < y.Length
                                    ? -1 : 0;
    }
}

sealed class FileDateComparer : IComparer<IFileEntry?>
{
    public int Compare(IFileEntry? x, IFileEntry? y)
    {
        return x == null && y == null
                    ? 0 : x == null
                    ? -1 : y == null
                    ? 1 : x.WriteDateTime > y.WriteDateTime
                    ? 1 : x.WriteDateTime < y.WriteDateTime
                    ? -1 : 0;
    }
}
