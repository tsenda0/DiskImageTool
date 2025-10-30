
namespace DiskImageTool;

/// <summary>
/// ディスクイメージファイル読み込みのインターフェース
/// </summary>
public interface IImageReader : IDisposable
{
    /// <summary>
    /// 読み込まれたディスクイメージのバッファを取得
    /// </summary>
    /// <returns></returns>
    byte[]? GetBuffer();
    /// <summary>
    /// イメージファイルを開く
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    bool OpenImage(string file);
    /// <summary>
    /// イメージファイルを開く
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    bool OpenImage(Stream stream);
}
