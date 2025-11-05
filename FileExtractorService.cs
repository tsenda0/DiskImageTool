using System.Diagnostics;
using System.IO;

namespace DiskImageTool;

/// <summary>
/// ファイル抽出処理を担当するサービスクラス
/// </summary>
public class FileExtractorService() : IDisposable
{
    [Conditional("DEBUG")]
    static void debugDelay()
    {
        Thread.Sleep(5);
    }

    CancellationTokenSource? cancellationTokenSource;

    public (Task<ExtractReport>, CancellationTokenSource cancellationTokenSource) ExtractFilesTaskAsync(
        IEnumerable<IFileEntry> files,
        string destinationPath,
        IFileSystem fileSystem,
        bool isUTC,
        IProgress<ExtractReport> progress
        )
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

        cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        var task = Task.Run(async () =>
        {
            foreach (var file in files)
            {
                if (token.IsCancellationRequested)
                {
                    report.IsCanceled = token.IsCancellationRequested;
                    token.ThrowIfCancellationRequested();
                }

                try
                {
                    report.CurrentFileName = file.Name;
                    report.CurrentFileLength = file.Length;
                    progress.Report(report);

                    var outPath = Path.Combine(destinationPath, file.Name);
                    await fileSystem.ExtractFile(file, outPath, isUTC, token);

                    // 成功後に加算
                    report.SuccessCount++;
                    report.CompletedBytes += file.Length;

                    // DEBUG
                    debugDelay();
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

        return (task, cancellationTokenSource);
    }

    public void Dispose()
    {
        Debug.WriteLine("FileExtractorService: disposing");

        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;

        GC.SuppressFinalize(this);
    }
}
