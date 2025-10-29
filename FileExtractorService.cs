namespace DiskImageTool;

/// <summary>
/// ファイル抽出処理を担当するサービスクラス
/// </summary>
public class FileExtractorService(IImageExtractor imageExtractor)
{
    private readonly IImageExtractor _imageExtractor = imageExtractor;

    public Task<ExtractReport> ExtractFilesAsync(List<FatFileEntry> files, string destinationPath, CancellationToken token, IProgress<ExtractReport> progress)
    {
        return Task.Run(() =>
        {
            var report = new ExtractReport()
            {
                TotalCount = files.Count,
                TotalBytes = files.Sum(f => f.Length),
                CompletedBytes = 0,
                SuccessCount = 0,
                ErrorCount = 0,
                IsCanceled = false
            };

            foreach (var file in files)
            {
                if (token.IsCancellationRequested)
                {
                    report.IsCanceled = true;
                    break;
                }

                try
                {
                    report.CurrentFileName = file.Name;
                    report.CurrentFileLength = file.Length;
                    progress.Report(report);

                    _imageExtractor.ExtractFile(file, destinationPath);

                    // 成功後に加算
                    report.SuccessCount++;
                    report.CompletedBytes += file.Length;
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
