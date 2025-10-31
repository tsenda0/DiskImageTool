using System.Diagnostics;

namespace DiskImageTool;

public partial class FormDiskImageTool : Form
{
    private readonly IImageReaderFactory imageReaderFactory;
    IImageReader? imageReader;

    private readonly IFileSystemFactory fsFactory;
    IFileSystem? fileSystem;

    SortOrder sortOrder = SortOrder.Unknown;
    SortOrder lastSortOrder = SortOrder.Name;
    int sortColumn;
    SortDirection sortDirection = SortDirection.Ascending;

    readonly Dictionary<SortOrder, IComparer<IFileEntry?>> sorterMap = new() {
        { SortOrder.Unknown, new FileNameComparer() },
        { SortOrder.Name, new FileNameComparer() },
        { SortOrder.Size, new FileSizeComparer() },
        { SortOrder.Date, new FileDateComparer() },
    };

    CancellationTokenSource? cancellationTokenSource;
    Task? extractTask;

    public FormDiskImageTool(IImageReaderFactory imageReaderFactory, IFileSystemFactory fsFactory)
    {
        InitializeComponent();
        initFileDialog();

        this.imageReaderFactory = imageReaderFactory;
        this.fsFactory = fsFactory;

        updateListView();
    }

    void initFileDialog()
    {
        openFileDialog1.CheckFileExists = true;
        openFileDialog1.Multiselect = false;
        openFileDialog1.CheckPathExists = true;
        openFileDialog1.Filter = @"DCUイメージファイル(*.DCU)|*.DCU|RAWイメージファイル(*.IMG)|*.IMG|LZHアーカイブ(*.LZH)|*.LZH|すべてのファイル(*.*)|*.*";
        openFileDialog1.FilterIndex = 3;
        openFileDialog1.RestoreDirectory = true;
        openFileDialog1.FileName = "";
    }

    private void openImageFile_Click(object sender, EventArgs e)
    {
        var result = openFileDialog1.ShowDialog(this);
        if (result == DialogResult.Cancel) return;

        string imageFile = openFileDialog1.FileName;
        ImageFormat imageFormat = openFileDialog1.FilterIndex switch
        {
            1 => ImageFormat.DCU,
            2 => ImageFormat.Raw,
            3 => ImageFormat.LZH,
            _ => ImageFormat.Raw, //Rawとして開く
        };

        if (imageFormat is ImageFormat.DCU or ImageFormat.LZH) checkIsUTC.Checked = false;

        labelFileName.Text = "";
        labelStatus.Text = "";
        listViewFiles.Items.Clear();
        try
        {
            imageReader?.Dispose(); // 既存のReaderを破棄

            imageReader = imageReaderFactory.Create(imageFormat);
            if (!imageReader.OpenImage(imageFile))
            {
                return;
            }

            labelFileName.Text = imageFile;

            fileSystem?.Dispose();

            fileSystem = fsFactory.Create(imageReader, FileSystemType.FAT);

            updateListView();
        }
        catch (Exception ex)
        {
            imageReader?.Dispose();
            imageReader = null;
            fileSystem?.Dispose();
            fileSystem = null;
            MessageBox.Show($"エラー: {ex.Message}");
        }
    }

    private IEnumerable<IFileEntry> getFiles()
    {
        if (imageReader == null || fileSystem == null)
        {
            return [];
        }

        var root = fileSystem?.GetRoot();

        return root?.SubEntries ?? [];
    }

    static DateTime utcToLocalTime(DateTime utc)
    {
        return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
    }

    void updateListView()
    {
        try
        {
            listViewFiles.Items.Clear();

            var files = getFiles();
            var sortedLvItems = sortListViewItems(files);

            listViewFiles.Items.AddRange(sortedLvItems.ToArray());

            var fatType = (fileSystem is FatFileSystem fatFs) ? $" / {fatFs.FatType}" : "";

            var countStr = fileSystem != null
                ? $" ({fileSystem.ImageSizeBytes / 1024.0 / 1024.0:0.00}MBフォーマット{fatType} / {sortedLvItems.Count()}個の項目)"
                : "";
            labelStatus.Text = $@"{imageReader?.OpenFileName}{countStr}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エラー: {ex.Message}");
        }
    }

    private IOrderedEnumerable<ListViewItem> sortListViewItems(IEnumerable<IFileEntry> files)
    {
        var lvitems = files.Select(f =>
        {
            var item = new ListViewItem(f.Name);
            item.SubItems.Add(new ListViewItem.ListViewSubItem(item, $"{f.Length:N0}")); // N0 は桁区切り付き数値を表す
            var date = $"{(checkIsUTC.Checked ? utcToLocalTime(f.WriteDateTime) : f.WriteDateTime):yyyy-MM-dd HH:mm:ss}";
            item.SubItems.Add(new ListViewItem.ListViewSubItem(item, date));
            item.Tag = f;
            return item;
        });

        var sortedLvItems = sortDirection == SortDirection.Descending
            ? lvitems.OrderByDescending(f => f.Tag as IFileEntry, sorterMap[sortOrder])
            : lvitems.OrderBy(f => f.Tag as IFileEntry, sorterMap[sortOrder]);

        return sortedLvItems;
    }

    private async void extractAll_Click(object sender, EventArgs e)
    {
        if (imageReader == null || fileSystem == null)
        {
            MessageBox.Show("イメージが開かれていません");
            return;
        }

        try
        {
            var files = getFiles();
            if (!files.Any())
            {
                MessageBox.Show("イメージにファイルがありません。");
                return;
            }

            var result = folderBrowserDialog1.ShowDialog(this);
            if (result == DialogResult.Cancel) return;

            await startExtractAsync(files);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エラー: {ex.Message}");
        }

    }

    private async void extract_Click(object sender, EventArgs e)
    {
        if (imageReader == null || fileSystem == null)
        {
            MessageBox.Show("イメージが開かれていません");
            return;
        }

        try
        {
            var files = getFiles();
            if (!files.Any())
            {
                MessageBox.Show("イメージにファイルがありません。");
                return;
            }

            // 以下のToArray()は無くてもコンパイルは通るが
            // 実行時にエラーとなるので注意(スレッドを跨いだアクセス)
            var checkedFiles = listViewFiles.CheckedItems
                .Cast<ListViewItem>()
                .Select(item => item.Tag as IFileEntry)
                .Where(file => file != null)
                .Cast<IFileEntry>()
                .ToArray();

            if (checkedFiles.Length == 0)
            {
                MessageBox.Show("ファイルが選択されていません。");
                return;
            }

            var result = folderBrowserDialog1.ShowDialog(this);
            if (result == DialogResult.Cancel) return;

            await startExtractAsync(checkedFiles);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エラー: {ex.Message}");
        }
    }

    private async Task startExtractAsync(IEnumerable<IFileEntry> files)
    {
        cancellationTokenSource = new CancellationTokenSource();

        using var formProgress = createProgressForm(files.Count(), () => cancellationTokenSource.Cancel());
        var reporter = formProgress.GetReporter();
        formProgress.Show(this);

        try
        {
            extractTask = startExtractTaskAsync(files, reporter, cancellationTokenSource.Token);
            await extractTask;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("UI: operation canceled(OperationCanceledException catched)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UI: exception catched: {ex.Message}");
            MessageBox.Show($"エラー: {ex.Message}");
        }
        finally
        {
            extractTask = null;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }

        showExtractionResult(formProgress.Report);
    }

    async Task startExtractTaskAsync(
        IEnumerable<IFileEntry> fileList,
        Progress<ExtractReport> reporter,
        CancellationToken token)
    {
        if (imageReader == null || fileSystem == null)
        {
            throw new InvalidOperationException("イメージが開かれていません");
        }

        var extractorService = new FileExtractorService(fileSystem);
        await Task.Run(() => extractorService.ExtractFilesAsync(
                fileList,
                folderBrowserDialog1.SelectedPath,
                checkIsUTC.Checked,
                token,
                reporter)
        , token);
    }

    private FormProgress createProgressForm(int maxCount, Action cancelAction)
    {
        FormProgress formProgress = new()
        {
            MaxCount = maxCount,
            Value = 0,
            Title = "ファイルを抽出しています",
            Opacity = 0
        };

        formProgress.CancelClicked += cancelAction;

        formProgress.Shown += (s, e) =>
        {
            formProgress.Left = Left + (Width - formProgress.Width) / 2;
            formProgress.Top = Top + (Height - formProgress.Height) / 2;
            formProgress.Opacity = 1;
        };

        return formProgress;
    }

    private static void showExtractionResult(ExtractReport? result)
    {
        if (result == null) return;

        var canceledMessage = result.IsCanceled ? "キャンセルしました。" : "";
        var successMessage = $"{result.SuccessCount}個のファイルを抽出しました。";
        var errorMessage = result.ErrorCount > 0 ? $"{result.ErrorCount}件のエラーがありました。" : "";

        var message = $"{canceledMessage}{successMessage}{errorMessage}";

        MessageBox.Show(message);
    }

    bool isClosing;

    private async void formDiskImageTool_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (isClosing) return;

        if (extractTask != null)
        {
            e.Cancel = true;

            var res = MessageBox.Show("ファイルを抽出中です。終了してよろしいですか?", "終了", MessageBoxButtons.OKCancel);
            if (res == DialogResult.Cancel)
            {
                return;
            }

            isClosing = true;

            try
            {
                if (cancellationTokenSource != null)
                {
                    Debug.WriteLine("\nform closing: cancelling task");
                    cancellationTokenSource.Cancel();
                }

                // UIスレッドをブロックしないように非同期で待機
                if (extractTask != null && !extractTask.IsCompleted)
                {
                    Debug.WriteLine("\nform closing: awaiting task");
                    await extractTask;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("\nform closing: operation canceled(OperationCanceledException catched)");
            }

            // close form
            Close();
        }
    }

    private void buttonFATInfo_Click(object sender, EventArgs e)
    {
        if (fileSystem == null)
        {
            MessageBox.Show("イメージが開かれていません");
            return;
        }

        FormFileSystemInfo formInfo = new FormFileSystemInfo(fileSystem);
        formInfo.ShowDialog(this);
    }

    private void listViewFiles_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        sortOrder = e.Column switch
        {
            1 => SortOrder.Size, //size
            2 => SortOrder.Date, //date
            _ => SortOrder.Name, //name
        };

        sortDirection = sortOrder != lastSortOrder
            ? SortDirection.Ascending
            : sortDirection == SortDirection.Descending ? SortDirection.Ascending : SortDirection.Descending;
        sortColumn = e.Column;
        lastSortOrder = sortOrder;

        updateListView();
    }

    private void checkIsUTC_Click(object sender, EventArgs e)
    {
        updateListView();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        if (disposing)
        {
            imageReader?.Dispose();
            imageReader = null;
            fileSystem?.Dispose();
            fileSystem = null;
        }

        base.Dispose(disposing);
    }

    const int TRIANGLE_SIZE = 8;
    const int TRIANGLE_OFFSET = 10;

    private void listViewFiles_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
    {
        e.Graphics.FillRectangle(SystemBrushes.Control, e.Bounds);
        e.DrawText(
            TextFormatFlags.WordEllipsis
            | TextFormatFlags.SingleLine
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.HorizontalCenter);
        if (e.ColumnIndex == sortColumn)
        {
            drawSortTriangle(e.Bounds, e.Graphics);
        }
    }

    void drawSortTriangle(Rectangle bounds, Graphics g)
    {
        var x = bounds.X + bounds.Width - TRIANGLE_OFFSET;
        var y = bounds.Y + bounds.Height / 2;
        var pts = new List<Point>
            {
                new(x, y + TRIANGLE_SIZE / 2 * (sortDirection == SortDirection.Ascending ? -1 : 1)),
                new(x - TRIANGLE_SIZE / 2, y - TRIANGLE_SIZE / 2 * (sortDirection == SortDirection.Ascending ? -1 : 1)),
                new(x + TRIANGLE_SIZE / 2, y - TRIANGLE_SIZE / 2 * (sortDirection == SortDirection.Ascending ? -1 : 1))
            };
        g.FillPolygon(SystemBrushes.ControlText, pts.ToArray());
    }

    private void listViewFiles_DrawItem(object sender, DrawListViewItemEventArgs e)
    {
        e.DrawDefault = true;
    }

    private void buttonVersionInfo_Click(object sender, EventArgs e)
    {
        var tok = Application.ProductVersion.Split("+");
        var ver = tok.Length > 1
            ? $"Version: {tok[0]} / Commit: {tok[1][..8]}"
            : Application.ProductVersion;

        MessageBox.Show(
            $"{Application.ProductName}\n"
            + $"{ver}\n"
            + $"2025-10 {Application.CompanyName}");
    }
}
