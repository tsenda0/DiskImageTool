namespace DiskImageTool;

/// <summary>
/// IImageExtractorのインスタンスを生成するファクトリクラス
/// </summary>
public class ImageExtractorFactory : IImageExtractorFactory
{
    public IImageExtractor Create(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.DCU => new DcuExtractor(),
            ImageFormat.Raw => new RawExtractor(),
            ImageFormat.Unknown => throw new NotSupportedException($"Unsupported image format: {format}"),
            _ => throw new NotSupportedException($"Unsupported image format: {format}")
        };
    }
}

public interface IImageExtractorFactory
{
    IImageExtractor Create(ImageFormat format);
}
