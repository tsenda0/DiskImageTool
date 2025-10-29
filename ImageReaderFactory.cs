namespace DiskImageTool;
public enum ImageFormat
{
    Unknown = 0,
    DCU = 1,
    Raw = 2,
    LZH = 3,
}

/// <summary>
/// IImageReaderのインスタンスを生成するファクトリクラス
/// </summary>
public class ImageReaderFactory : IImageReaderFactory
{
    public IImageReader Create(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.DCU => new DcuReader(),
            ImageFormat.Raw => new RawReader(),
            ImageFormat.LZH => new LzhDcuReader(),
            ImageFormat.Unknown => throw new InvalidOperationException("フォーマットが不明です"),
            _ => throw new InvalidOperationException("フォーマットが不明です"),
        };
    }
}

public interface IImageReaderFactory
{
    IImageReader Create(ImageFormat format);
}
