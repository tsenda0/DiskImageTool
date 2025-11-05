using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace DiskImageTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable
{
    public MainWindow()
    {
        InitializeComponent();

        Closing += mainWindow_Closing;
        checkBoxIsUTC.Click += checkBoxIsUTC_Click;
        IFileEntry[] data = [];
        listView.ItemsSource = data;
    }

    private void checkBoxIsUTC_Click(object sender, RoutedEventArgs e)
    {
        if (fileSystem == null) return;

        rootEntry = fileSystem.GetRoot(checkBoxIsUTC.IsChecked.HasValue && checkBoxIsUTC.IsChecked.Value);
        updateListView(rootEntry.SubEntries);
    }

    bool closing;
    private async void mainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (closing) return;

        Debug.WriteLine("Closing: closing window");

        closing = true;

        if (extractTask != null) e.Cancel = true;
        await cancelExtractTask();
    }

    async Task<ExtractReport?> cancelExtractTask()
    {
        if (extractTask != null)
        {
            try
            {
                if (cancellationTokenSource != null)
                {
                    Debug.WriteLine("cancelExtractTask: cancelling extract task:");
                    cancellationTokenSource?.Cancel();
                }

                if (extractTask != null)
                {
                    var res = await extractTask;
                    Debug.WriteLine("cancelExtractTask: extract task exited");
                    return res;
                }
            }
            catch (OperationCanceledException)
            {
                //canceled
                Debug.WriteLine("cancelExtractTask: extract task cancelled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"cancelExtractTask: exception catched: {ex.Message}");
                MessageBox.Show($"エラー: {ex.Message}");
            }
            finally
            {
                Close();

                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;

                extractTask = null;
            }
        }

        return null;
    }

    IFileSystem? fileSystem;
    IImageReader? imageReader;
    IFileEntry? rootEntry;
    CancellationTokenSource? cancellationTokenSource;
    Task<ExtractReport>? extractTask;

    private void selectImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "ファイルを選択",
            Filter = "すべてのファイル (*.*)|*.*",
            CheckFileExists = true,
        };

        var res = dialog.ShowDialog(this);

        if (!res.HasValue || !res.Value) return;

        try
        {
            string path = dialog.FileName;
            imageReader = dialog.FileName.EndsWith(".dcu", StringComparison.OrdinalIgnoreCase)
                ? new DcuReader()
                : dialog.FileName.EndsWith(".lzh", StringComparison.OrdinalIgnoreCase)
                    ? new LzhDcuReader(new ArchiveImageSelectWindpw(this))
                    : new RawReader();

            if (imageReader.OpenImage(path))
            {
                FileSystemFactory factory = new FileSystemFactory();
                fileSystem = factory.Create(imageReader, FileSystemType.FAT);
                rootEntry = fileSystem.GetRoot(checkBoxIsUTC.IsChecked.HasValue && checkBoxIsUTC.IsChecked.Value);
                updateFileName(path);
                updateListView(rootEntry.SubEntries);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"エラー: {ex.Message}");
        }
    }

    void updateFileName(string path)
    {
        label.Content = path;
    }

    void updateListView(IEnumerable<IFileEntry>? files)
    {
        listView.ItemsSource = files;
    }

    private async void extractAll_Click(object sender, RoutedEventArgs e)
    {
        if (rootEntry?.SubEntries == null)
        {
            MessageBox.Show(this, "イメージにファイルがありません");
            return;
        }

        var ofd = new OpenFolderDialog
        {
            Title = "抽出先を選択してください"
        };

        var ofdResult = ofd.ShowDialog(this);
        if (!ofdResult.HasValue || !ofdResult.Value) return;
        var destPath = ofd.FolderName;

        ExtractReport? rep = null;
        try
        {
            rep = await startExtractAsync(rootEntry.SubEntries, destPath);

            Debug.WriteLine("extractAll: extract task exited");
            if (rep != null && !closing)
            {
                var canceled = rep.IsCanceled ? "キャンセルしました。" : "";

                MessageBox.Show(this,
                    canceled + $"{rep.SuccessCount}個のファイルを抽出しました。"
                    + (rep.ErrorCount > 0 ? $"({rep.ErrorCount}のエラーがありました。)" : ""));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"extractAll: exception catched: {ex.Message}");

            if (rep != null)
            {
                MessageBox.Show(this,
                    $"エラー: {ex.Message}"
                    + $"({rep.SuccessCount}個のファイルを抽出しました。"
                    + (rep.ErrorCount > 0 ? $"{rep.ErrorCount}のエラーがありました。" : "")
                    + ")");
            }
            else
            {
                MessageBox.Show(this,
                    $"エラー: {ex.Message}");
            }
        }
        finally
        {
            extractTask = null;
        }
    }

    ProgressWindow createProgress()
    {
        var progWindow = new ProgressWindow
        {
            Owner = this
        };

        progWindow.Canceled += (s, e) => cancellationTokenSource?.Cancel();

        return progWindow;
    }

    private async Task<ExtractReport?> startExtractAsync(IEnumerable<IFileEntry> files, string destPath)
    {
        if (fileSystem == null)
        {
            //MessageBox.Show(this, "イメージが開かれていません");
            return null;
        }

        cancellationTokenSource = new CancellationTokenSource();
        FileExtractorService svc = new FileExtractorService(fileSystem);

        var progWindow = createProgress();
        var prog = progWindow.GetReporter();
        prog.Report(null);
        progWindow.Show();

        try
        {
            extractTask = Task.Run(
                async () => await svc.ExtractFilesAsync(files, destPath, false, cancellationTokenSource.Token, prog));
            var res = await extractTask;

            Debug.WriteLine("startExtract: extraact task exited");

            return res;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("startExtract: extract task cancelled");
            if (progWindow?.Progress != null) progWindow.Progress.IsCanceled = true;
        }
        finally
        {
            progWindow.Close();

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            extractTask = null;
        }

        return progWindow.Progress;
    }

    public static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null) yield break;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                yield return match;

            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    private async void extractChecked_Click(object sender, RoutedEventArgs e)
    {
        if (rootEntry?.SubEntries == null)
        {
            MessageBox.Show(this, "イメージにファイルがありません");
            return;
        }

        List<IFileEntry> checkFiles = [];
        for (int i = 0; i < listView.Items.Count; i++)
        {
            if (listView.ItemContainerGenerator.ContainerFromIndex(i) is not ListViewItem item) continue;

            var cb = FindVisualChildren<CheckBox>(item).FirstOrDefault();
            if (cb != null && listView.Items[i] is IFileEntry entry)
            {
                if (cb.IsChecked.HasValue && cb.IsChecked.Value)
                {
                    checkFiles.Add(entry);
                }
            }
        }

        if (checkFiles.Count == 0)
        {
            MessageBox.Show(this, "ファイルが選択されていません");
            return;
        }

        var ofd = new OpenFolderDialog
        {
            Title = "抽出先を選択してください"
        };

        var ofdResult = ofd.ShowDialog(this);
        if (!ofdResult.HasValue || !ofdResult.Value) return;
        var destPath = ofd.FolderName;

        ExtractReport? rep = null;
        try
        {
            rep = await startExtractAsync(checkFiles, destPath);

            Debug.WriteLine("extractAll: extract task exited");
            if (rep != null && !closing)
            {
                var canceled = rep.IsCanceled ? "キャンセルしました。" : "";

                MessageBox.Show(this,
                    canceled + $"{rep.SuccessCount}個のファイルを抽出しました。"
                    + (rep.ErrorCount > 0 ? $"({rep.ErrorCount}のエラーがありました。)" : ""));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"extractAll: exception catched: {ex.Message}");

            if (rep != null)
            {
                MessageBox.Show(this,
                    $"エラー: {ex.Message}"
                    + $"({rep.SuccessCount}個のファイルを抽出しました。"
                    + (rep.ErrorCount > 0 ? $"{rep.ErrorCount}のエラーがありました。" : "")
                    + ")");
            }
            else
            {
                MessageBox.Show(this,
                    $"エラー: {ex.Message}");
            }
        }
        finally
        {
            extractTask = null;
        }
    }

    private void buttonClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
