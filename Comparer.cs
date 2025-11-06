using System.ComponentModel;
using System.IO;

namespace DiskImageTool;

public sealed class FileNameComparer(ListSortDirection direction) : System.Collections.IComparer
{
    readonly ListSortDirection direction = direction;

    public int Compare(object? x, object? y)
    {
        return SortByName(x, y, direction);
    }

    public static int SortByName(object? x, object? y, ListSortDirection direction)
    {
        CheckFileEntry? ex = x as CheckFileEntry;
        CheckFileEntry? ey = y as CheckFileEntry;

        if (ex == null && ey == null) return 0;
        if (ex == null) return -1;
        if (ey == null) return 1;

        string base_x = Path.GetFileNameWithoutExtension(ex.BaseEntry.Name);
        string base_y = Path.GetFileNameWithoutExtension(ey.BaseEntry.Name);

        return (int.TryParse(base_x, out int ix) && int.TryParse(base_y, out int iy)
            ? ix == iy ? 0 : ix > iy ? 1 : -1
            : string.CompareOrdinal(base_x, base_y)) * (direction == ListSortDirection.Ascending ? 1 : -1);
    }
}

public sealed class FileLengthComparer(ListSortDirection direction) : System.Collections.IComparer
{
    readonly ListSortDirection direction = direction;

    public int Compare(object? x, object? y)
    {
        return SortByFileLength(x, y, direction);
    }

    public static int SortByFileLength(object? x, object? y, ListSortDirection direction)
    {
        CheckFileEntry? ex = x as CheckFileEntry;
        CheckFileEntry? ey = y as CheckFileEntry;

#pragma warning disable IDE0046 // 条件式が複雑になりすぎるため無効にする
        if (ex == null && ey == null) return 0;
        if (ex == null) return -1;
        if (ey == null) return 1;
#pragma warning restore IDE0046

        return (ex.BaseEntry.Length > ey.BaseEntry.Length
            ? 1
            : ex.BaseEntry.Length < ey.BaseEntry.Length
                ? -1
                : 0)
            * (direction == ListSortDirection.Ascending ? 1 : -1);
    }
}

public sealed class WriteDateTimeComparer(ListSortDirection direction) : System.Collections.IComparer
{
    readonly ListSortDirection direction = direction;

    public int Compare(object? x, object? y)
    {
        return SortByWriteDateTime(x, y, direction);
    }

    public static int SortByWriteDateTime(object? x, object? y, ListSortDirection direction)
    {
        CheckFileEntry? ex = x as CheckFileEntry;
        CheckFileEntry? ey = y as CheckFileEntry;

#pragma warning disable IDE0046 // 条件式が複雑になりすぎるため無効にする
        if (ex == null && ey == null) return 0;
        if (ex == null) return -1;
        if (ey == null) return 1;
#pragma warning restore IDE0046

        return (ex.BaseEntry.WriteDateTime > ey.BaseEntry.WriteDateTime
            ? 1
            : ex.BaseEntry.WriteDateTime < ey.BaseEntry.WriteDateTime
                ? -1
                : 0)
            * (direction == ListSortDirection.Ascending ? 1 : -1);
    }
}

public sealed class CheckedComparer(ListSortDirection direction) : System.Collections.IComparer
{
    readonly ListSortDirection direction = direction;

    public int Compare(object? x, object? y)
    {
        return SortByChecked(x, y, direction);
    }

    public static int SortByChecked(object? x, object? y, ListSortDirection direction)
    {
        bool bx = x is CheckFileEntry ex && ex.Checked;
        bool by = y is CheckFileEntry ey && ey.Checked;

        int ret = 0;
        if (bx && !by) ret = -1;
        if (!bx && by) ret = 1;

        return ret == 0
            ? FileNameComparer.SortByName(x, y, direction)
            : (ret * (direction == ListSortDirection.Ascending ? 1 : -1));
    }
}
