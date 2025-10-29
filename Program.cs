namespace DiskImageTool;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        // 依存性を注入してフォームを起動
        IImageReaderFactory readerFactory = new ImageReaderFactory();
        IFileSystemFactory fsFactory = new FileSystemFactory();

        Application.Run(new FormDiskImageTool(readerFactory, fsFactory));
    }
}
