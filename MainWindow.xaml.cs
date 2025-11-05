using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace DiskImageTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    IFileSystem? fileSystem;
    IImageReader? imageReader;
    IFileEntry? rootEntry;
    CancellationTokenSource? cancellationTokenSource;
    Task<ExtractReport>? extractTask;
    bool closing;

    public MainWindow()
    {
        InitializeComponent();

        Closing += mainWindow_Closing;
        Closed += mainWindow_Closed;
        checkBoxIsUTC.Click += checkBoxIsUTC_Click;
        IFileEntry[] data = [];

        updateFileName();
        updateListView(data);
    }

    private async void mainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        Debug.WriteLine("Closing: closing window");

        if (closing) return;

        if (extractTask != null)
        {
            closing = true;
            e.Cancel = true;
            await cancelExtractTask();
            Close();
        }
    }

    private void mainWindow_Closed(object? sender, EventArgs e)
    {
        Debug.WriteLine("Closed: window closed");

        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
    }

    private void checkBoxIsUTC_Click(object sender, RoutedEventArgs e)
    {
        if (fileSystem == null) return;

        rootEntry = fileSystem.GetRoot(checkBoxIsUTC.IsChecked == true);
        updateListView(rootEntry.SubEntries);
    }

    async Task<ExtractReport?> cancelExtractTask()
    {
        if (extractTask == null) return null;

        try
        {
            if (cancellationTokenSource != null)
            {
                Debug.WriteLine("cancelExtractTask: cancelling extract task:");
                cancellationTokenSource.Cancel();
            }

            var res = await extractTask;
            Debug.WriteLine("cancelExtractTask: extract task exited");
            return res;
        }
        catch (OperationCanceledException)
        {
            //canceled
            Debug.WriteLine("cancelExtractTask: extract task cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"cancelExtractTask: caught exception: {ex.Message}");
            MessageBox.Show($"エラー: {ex.Message}");
        }
        finally
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            extractTask = null;
        }

        return null;
    }

    private void selectImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "ファイルを選択",
            Filter = "すべてのファイル (*.*)|*.*",
            CheckFileExists = true,
        };

        var res = dialog.ShowDialog(this);

        if (res != true) return;

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
                rootEntry = fileSystem.GetRoot(checkBoxIsUTC.IsChecked == true);

                updateFileName(path);
                updateListView(rootEntry.SubEntries);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"エラー: {ex.Message}");
        }
    }

    void updateFileName(string? path = null)
    {
        labelFileName.Content = path ?? "ファイルを選択してください";
        labelFileName.Foreground = path != null
            ? SystemColors.ControlTextBrush
            : SystemColors.GrayTextBrush;
    }

    void updateListView(IEnumerable<IFileEntry>? files)
    {
        listViewFiles.ItemsSource = files;
        textBoxStatus.Text = $"{files?.Count():N0}個のファイル / {files?.Sum(f => f.Length):N0} bytes";
        propertyGrid.SelectedObject = fileSystem;
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
        if (ofdResult != true) return;
        var destPath = ofd.FolderName;

        await startExtractAsync(rootEntry.SubEntries, destPath);
    }

    private async void extractChecked_Click(object sender, RoutedEventArgs e)
    {
        if (rootEntry?.SubEntries == null)
        {
            MessageBox.Show(this, "イメージにファイルがありません");
            return;
        }

        List<IFileEntry> checkFiles = [];
        for (int i = 0; i < listViewFiles.Items.Count; i++)
        {
            if (listViewFiles.ItemContainerGenerator.ContainerFromIndex(i) is not ListViewItem item) continue;

            var cb = FindVisualChildren<CheckBox>(item).FirstOrDefault();
            if (cb != null && listViewFiles.Items[i] is IFileEntry entry)
            {
                if (cb.IsChecked == true)
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
        if (ofdResult != true) return;
        var destPath = ofd.FolderName;

        await startExtractAsync(checkFiles, destPath);
    }

    private async Task startExtractAsync(IEnumerable<IFileEntry> files, string destPath)
    {
        if (fileSystem == null)
        {
            MessageBox.Show(this, "イメージが開かれていません");
            return;
        }

        using FileExtractorService svc = new FileExtractorService();
        var progWindow = createProgress();
        progWindow.Owner = this;
        var progress = progWindow.GetReporter();
        progress.Report(null);
        progWindow.Show();

        try
        {
            (extractTask, cancellationTokenSource) = svc.ExtractFilesTaskAsync(
                files, destPath, fileSystem, checkBoxIsUTC.IsChecked == true, progress);
            var rep = await extractTask;

            Debug.WriteLine("startExtractAsync: extract task exited");

            if (rep != null)
            {
                if (closing) return;

                var canceled = rep.IsCanceled ? "キャンセルしました。\n" : "";
                MessageBox.Show(this,
                    canceled + $"{rep.SuccessCount}個のファイルを抽出しました。"
                    + (rep.ErrorCount > 0 ? $"({rep.ErrorCount}のエラーがありました。)" : ""));
            }
            else
            {
                MessageBox.Show(this, "エラーが発生しました。");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("startExtractAsync: extract task cancelled");
            if (closing) return;

            var msg =
                progWindow.Progress != null
                    ? ($"{progWindow.Progress.SuccessCount}個のファイルを抽出しました。"
                        + (progWindow.Progress.ErrorCount > 0 ? $"({progWindow.Progress.ErrorCount}のエラーがありました。)" : ""))
                    : "";
            MessageBox.Show(this, "キャンセルしました。\n" + msg);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"startExtractAsync: exception catched: {ex.Message}");

            var msg =
                progWindow.Progress != null
                    ? ($"{progWindow.Progress.SuccessCount}個のファイルを抽出しました。"
                        + (progWindow.Progress.ErrorCount > 0 ? $"({progWindow.Progress.ErrorCount}のエラーがありました。)" : ""))
                    : "";
            MessageBox.Show(this,
                $"エラーが発生しました: {ex.Message}\n" + msg);
        }
        finally
        {
            Debug.WriteLine($"startExtractAsync: disposing");

            progWindow.Close();

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            extractTask = null;
        }
    }
    ProgressWindow createProgress()
    {
        var progWindow = new ProgressWindow();

        progWindow.Canceled += (s, e) => cancellationTokenSource?.Cancel();

        return progWindow;
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

    private void buttonClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
