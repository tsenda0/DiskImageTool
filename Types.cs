namespace DiskImageTool;

public enum SortOrder
{
    Unknown = 0,
    Name = 1,
    Size = 2,
    Date = 3,
}

public enum SortDirection
{
    Ascending = 0,
    Descending = 1,
}

/// <summary>
/// Represents the progress of a file extraction operation.
/// </summary>
public sealed class ExtractReport
{
    public int? TotalCount { get; set; }
    public int? SuccessCount { get; set; }
    public int? ErrorCount { get; set; }
    public long? TotalBytes { get; set; }
    public long? CompletedBytes { get; set; }
    public string? CurrentFileName { get; set; } = "";
    public uint? CurrentFileLength { get; set; }
    public bool IsCanceled { get; set; }
}
