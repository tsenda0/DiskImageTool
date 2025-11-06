using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace DiskImageTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    IFileSystem? fileSystem;
    IImageReader? imageReader;
    List<CheckFileEntry> allFiles = [];

    CancellationTokenSource? cancellationTokenSource;
    Task<ExtractReport>? extractTask;
    bool closing;

    private GridViewColumnHeader? lastHeaderClicked;
    private ListSortDirection lastDirection = ListSortDirection.Ascending;

    public MainWindow()
    {
        InitializeComponent();

        Closing += mainWindow_Closing;
        Closed += mainWindow_Closed;
        checkBoxIsUTC.Click += checkBoxIsUTC_Click;

        updateFileName();
        updateListView();
        updateStatus();
    }

    private async void mainWindow_Closing(object? sender, CancelEventArgs e)
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

    List<CheckFileEntry> createEntries(IFileEntry? rootEntry)
    {
        return rootEntry?.SubEntries != null
            ? rootEntry.SubEntries.Select(i =>
            {
                var e = new CheckFileEntry(i);
                e.PropertyChanged += e_PropertyChanged;
                return e;
            }).ToList()
            : [];
    }

    private void e_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        updateStatus();
    }

    private void checkBoxIsUTC_Click(object sender, RoutedEventArgs e)
    {
        if (fileSystem == null) return;

        var rootEntry = fileSystem.GetRoot(checkBoxIsUTC.IsChecked == true);
        allFiles = createEntries(rootEntry);

        updateListView();
        updateStatus();
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
                var rootEntry = fileSystem.GetRoot(checkBoxIsUTC.IsChecked == true);
                allFiles = createEntries(rootEntry);

                updateFileName(path);
                updateListView();
                updateStatus();
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

    void updateListView()
    {
        listViewFiles.ItemsSource = allFiles;
        propertyGrid.SelectedObject = fileSystem;
    }

    void updateStatus()
    {
        var checkedEntries = allFiles.Where(i => i.Checked);

        textBoxStatus.Text = $"{allFiles.Count:N0}個のファイル({allFiles.Sum(f => f.BaseEntry.Length):N0} bytes)"
            + $" / {checkedEntries.Count():N0}個のファイル({checkedEntries.Sum(f => f.BaseEntry.Length):N0} bytes)を選択中";
    }

    private async void extractAll_Click(object sender, RoutedEventArgs e)
    {
        if (allFiles.Count == 0)
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

        await startExtractAsync(allFiles.Select(i => i.BaseEntry), destPath);
    }

    private async void extractChecked_Click(object sender, RoutedEventArgs e)
    {
        if (allFiles == null)
        {
            MessageBox.Show(this, "イメージにファイルがありません");
            return;
        }

        if (!allFiles.Any(i => i.Checked))
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

        await startExtractAsync(allFiles.Where(i => i.Checked).Select(i => i.BaseEntry), destPath);
    }

    private async Task startExtractAsync(IEnumerable<IFileEntry> files, string destPath)
    {
        if (fileSystem == null)
        {
            MessageBox.Show(this, "イメージが開かれていません");
            return;
        }

        using FileExtractorService svc = new FileExtractorService();
        var progWindow = createProgress(this);
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

                progWindow.IsCancelEnabled = false;

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

            progWindow.CloseProgress();

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            extractTask = null;
        }
    }

    ProgressWindow createProgress(Window? owner)
    {
        var progWindow = new ProgressWindow
        {
            Owner = owner
        };
        progWindow.Canceled += (s, e) => cancellationTokenSource?.Cancel();

        return progWindow;
    }

    private void buttonClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void gridViewColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader header) return;

        // AttachedProperty からキーを取得
        var sortKey = ColumnExtensions.GetSortKey(header.Column);
        if (string.IsNullOrEmpty(sortKey)) return; // キー未設定なら無視

        // 昇降切替
        var direction = (lastHeaderClicked == header && lastDirection == ListSortDirection.Ascending)
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending;

        sortListView(sortKey, direction);
        applySortArrow(header, direction);

        lastHeaderClicked = header;
        lastDirection = direction;
    }

    private void sortListView(string sortBy, ListSortDirection direction)
    {
        if (CollectionViewSource.GetDefaultView(listViewFiles.ItemsSource) is not ListCollectionView view)
            return;

        switch (sortBy)
        {
            case "Checked":
                view.CustomSort = new CheckedComparer(direction);
                break;
            case "Name":
                view.CustomSort = new FileNameComparer(direction);
                break;
            case "Length":
                view.CustomSort = new FileLengthComparer(direction);
                break;
            case "WriteDateTime":
                view.CustomSort = new WriteDateTimeComparer(direction);
                break;
            default:
                break;
        }
        view.Refresh();
    }

    private void applySortArrow(GridViewColumnHeader header, ListSortDirection dir)
    {
        // 前のヘッダを元に戻す
        if (lastHeaderClicked != null && lastHeaderClicked != header)
        {
            lastHeaderClicked.Content = lastHeaderClicked.Column.Header;
        }

        string baseText = header.Column.Header?.ToString() ?? "";
        string arrow = dir == ListSortDirection.Ascending ? " △" : " ▽";
        header.Content = baseText + arrow;
    }

    private void menuSelectAll_Click(object sender, RoutedEventArgs e)
    {
        for (var i = 0; i < allFiles.Count; i++)
        {
            allFiles[i].Checked = true;
        }
    }

    private void menuUnselectAll_Click(object sender, RoutedEventArgs e)
    {
        for (var i = 0; i < allFiles.Count; i++)
        {
            allFiles[i].Checked = false;
        }
    }
}

public static class ColumnExtensions
{
    public static readonly DependencyProperty SortKeyProperty =
        DependencyProperty.RegisterAttached(
            "SortKey",
            typeof(string),
            typeof(ColumnExtensions),
            new PropertyMetadata(null)
        );

    public static void SetSortKey(DependencyObject element, string value)
    {
        element.SetValue(SortKeyProperty, value);
    }

    public static string GetSortKey(DependencyObject element)
    {
        return (string)element.GetValue(SortKeyProperty);
    }
}
