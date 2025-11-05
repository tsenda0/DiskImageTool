using System.IO;

namespace DiskImageTool;

/// <summary>
/// ファイルシステム操作のインターフェース
/// </summary>
public interface IFileSystem : IDisposable
{
    /// <summary>
    /// ファイルシステムの種別
    /// </summary>
    FileSystemType FileSystemType { get; }
    /// <summary>
    /// イメージのID
    /// </summary>
    string Id { get; }
    /// <summary>
    /// イメージの名称(ボリュームラベル等)
    /// </summary>
    string Name { get; }
    /// <summary>
    /// イメージのサイズ
    /// </summary>
    int ImageSizeBytes { get; }
    /// <summary>
    /// ルートディレクトリのファイルを取得する
    /// </summary>
    /// <returns></returns>
    IFileEntry GetRoot(bool isUTC);
    /// <summary>
    /// ファイルを指定して開く
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    Stream OpenFile(IFileEntry file);
    /// <summary>
    /// パスを指定してファイルを開く
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    Stream OpenFile(string path);
    /// <summary>
    /// 指定したファイルシステム内のファイルを指定したパスへコピーする。
    /// </summary>
    /// <param name="file">ファイルシステム内のコピー元ファイル。</param>
    /// <param name="destFullPath">コピー先のフルパス。</param>
    /// <param name="isUTC">ファイルシステム内のファイルの日付がUTCかどうか。コピーの際にローカルタイムに変換されます。</param>
    Task ExtractFile(IFileEntry file, string destFullPath, bool isUTC, CancellationToken cancelToken);
}
