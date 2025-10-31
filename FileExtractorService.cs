namespace DiskImageTool;

/// <summary>
/// ファイル抽出処理を担当するサービスクラス
/// </summary>
public class FileExtractorService(IFileSystem fileSystem)
{
    private readonly IFileSystem fileSystem = fileSystem;

    public Task<ExtractReport> ExtractFilesAsync(
        IEnumerable<IFileEntry> files,
        string destinationPath,
        bool isUTC,
        CancellationToken token,
        IProgress<ExtractReport> progress)
    {
        var report = new ExtractReport()
        {
            TotalCount = files.Count(),
            TotalBytes = files.Sum(f => f.Length),
            CompletedBytes = 0,
            SuccessCount = 0,
            ErrorCount = 0,
            IsCanceled = false
        };

        return Task.Run(async () =>
        {
            foreach (var file in files)
            {
                if (token.IsCancellationRequested)
                {
                    report.IsCanceled = true;
                    progress.Report(report);
                    token.ThrowIfCancellationRequested();
                }

                try
                {
                    report.CurrentFileName = file.Name;
                    report.CurrentFileLength = file.Length;
                    progress.Report(report);

                    var outPath = Path.Combine(destinationPath, file.Name);
                    await fileSystem.ExtractFile(file, outPath, isUTC);

                    // 成功後に加算
                    report.SuccessCount++;
                    report.CompletedBytes += file.Length;

                    // DEBUG
                    Thread.Sleep(1);
                }
                catch (Exception)
                {
                    report.ErrorCount++;
                }
            }

            // 最終進捗報告
            progress.Report(report);

            return report;
        }, token);
    }
}
