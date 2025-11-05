namespace DiskImageTool;

/// <summary>
/// ファイルシステム内のファイルまたはディレクトリへのインターフェース
/// </summary>
public interface IFileEntry
{
    /// <summary>
    /// ディレクトリである場合、ディレクトリ内のファイルの一覧
    /// </summary>

    public IEnumerable<IFileEntry>? SubEntries { get; }
    /// <summary>
    /// ファイルまたはディレクトリの名称
    /// </summary>

    public string Name { get; }

    /// <summary>
    /// ファイルのサイズ。ディレクトリの場合は常に0となる。
    /// </summary>
    public uint Length { get; }
    /// <summary>
    /// ファイルまたはディレクトリの更新日時
    /// </summary>

    public DateTime WriteDateTime { get; }
}
