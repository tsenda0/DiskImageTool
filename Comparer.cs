using System.ComponentModel;
using System.IO;

namespace DiskImageTool;

sealed class FileNameComparer(ListSortDirection direction) : System.Collections.IComparer
{
    readonly ListSortDirection direction = direction;

    public int Compare(object? ox, object? oy)
    {
        IFileEntry? x = ox as IFileEntry;
        IFileEntry? y = oy as IFileEntry;

        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        string base_x = Path.GetFileNameWithoutExtension(x.Name);
        string base_y = Path.GetFileNameWithoutExtension(y.Name);

        return (int.TryParse(base_x, out int ix) && int.TryParse(base_y, out int iy)
            ? ix == iy ? 0 : ix > iy ? 1 : -1
            : string.CompareOrdinal(base_x, base_y)) * (direction == ListSortDirection.Ascending ? 1 : -1);
    }
}

sealed class FileSizeComparer(ListSortDirection direction) : System.Collections.IComparer
{
    readonly ListSortDirection direction = direction;

    public int Compare(object? ox, object? oy)
    {
        IFileEntry? x = ox as IFileEntry;
        IFileEntry? y = oy as IFileEntry;

        return (x == null && y == null
                    ? 0 : x == null
                        ? -1 : y == null
                            ? 1 : x.Length > y.Length
                                ? 1 : x.Length < y.Length
                                    ? -1 : 0) * (direction == ListSortDirection.Ascending ? 1 : -1);
    }
}

sealed class FileDateComparer(ListSortDirection direction) : System.Collections.IComparer
{
    readonly ListSortDirection direction = direction;

    public int Compare(object? ox, object? oy)
    {
        IFileEntry? x = ox as IFileEntry;
        IFileEntry? y = oy as IFileEntry;

        return (x == null && y == null
                    ? 0 : x == null
                    ? -1 : y == null
                    ? 1 : x.WriteDateTime > y.WriteDateTime
                    ? 1 : x.WriteDateTime < y.WriteDateTime
                    ? -1 : 0) * (direction == ListSortDirection.Ascending ? 1 : -1);
    }
}
