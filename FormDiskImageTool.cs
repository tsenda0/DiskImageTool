using System.Diagnostics;

namespace DiskImageTool;

public partial class FormDiskImageTool : Form
{
    private readonly IImageExtractorFactory _imageExtractorFactory;
    IImageExtractor? ImageExtractor;

    string ImageFile = "";
    ImageFormat ImageFormat = ImageFormat.Unknown;

    SortOrder SortOrder = SortOrder.Unknown;
    SortOrder lastSortOrder = SortOrder.Name;
    int SortColumn = 0;
    SortDirection SortDirection = SortDirection.Ascending;

    readonly Dictionary<SortOrder, IComparer<FatFileEntry?>> SorterMap = new() {
        { SortOrder.Unknown, new FileNameComparer() },
        { SortOrder.Name, new FileNameComparer() },
        { SortOrder.Size, new FileSizeComparer() },
        { SortOrder.Date, new FileDateComparer() },
    };

    CancellationTokenSource? CancellationTokenSource;
    Task? ExtractTask;

    public FormDiskImageTool(IImageExtractorFactory imageExtractorFactory)
    {
        InitializeComponent();
        _imageExtractorFactory = imageExtractorFactory;
        InitFileDialog();
        UpdateListView([]);
    }

    void InitFileDialog()
    {
        openFileDialog1.CheckFileExists = true;
        openFileDialog1.Multiselect = false;
        openFileDialog1.CheckPathExists = true;
        openFileDialog1.Filter = @"DCUイメージファイル(*.DCU)|*.DCU|RAWイメージファイル(*.IMG)|*.IMG|LZHアーカイブ(*.LZH)|*.LZH|すべてのファイル(*.*)|*.*";
        openFileDialog1.FilterIndex = 0;
        openFileDialog1.RestoreDirectory = true;
        openFileDialog1.FileName = "";
    }

    private void OpenImageFile_Click(object sender, EventArgs e)
    {
        var result = openFileDialog1.ShowDialog(this);
        if (result == DialogResult.Cancel) return;

        ImageFile = openFileDialog1.FileName;
        ImageFormat = openFileDialog1.FilterIndex switch
        {
            1 => ImageFormat.DCU,
            2 => ImageFormat.Raw,
            3 => ImageFormat.LZH,
            _ => ImageFormat.Raw, //Rawとして開く
        };

        if (ImageFile.Length == 0)
        {
            return;
        }

        if (ImageFormat is ImageFormat.DCU or ImageFormat.LZH) checkIsUTC.Checked = false;

        labelFileName.Text = "";
        labelStatus.Text = "";
        listViewFiles.Items.Clear();
        try
        {
            ImageExtractor?.Dispose(); // 既存のExtractorを破棄

            ImageExtractor = _imageExtractorFactory.Create(ImageFormat);
            if (ImageExtractor.OpenImage(ImageFile))
            {

                labelFileName.Text = ImageFile;

                var files = GetFiles();
                UpdateListView(files);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エラー: {ex.Message}");
        }
    }

    private IEnumerable<FatFileEntry> GetFiles()
    {
        if (ImageExtractor == null)
        {
            return [];
        }

        var root = ImageExtractor.GetRoot(checkIsUTC.Checked);
        return root?.GetFiles() ?? [];
    }

    void UpdateListView(IEnumerable<FatFileEntry> files)
    {
        var fileList = files.ToList();
        listViewFiles.Items.Clear();

        var lvitems = fileList.Select(f =>
        {
            var item = new ListViewItem(f.Name);
            item.SubItems.Add(new ListViewItem.ListViewSubItem(item, $"{f.Length:N0}")); // N0 は桁区切り付き数値を表す
            item.SubItems.Add(new ListViewItem.ListViewSubItem(item, $"{f.WriteDateTime:yyyy-MM-dd HH:mm:ss}"));
            item.Tag = f;
            return item;
        });

        var sortedLvItems = SortDirection == SortDirection.Descending
            ? lvitems.OrderByDescending(f => f.Tag as FatFileEntry, SorterMap[SortOrder])
            : lvitems.OrderBy(f => f.Tag as FatFileEntry, SorterMap[SortOrder]);

        listViewFiles.Items.AddRange([.. sortedLvItems]);
        labelStatus.Text = $@"{sortedLvItems.Count()}ファイル";
    }

    private async void ExtractAll_Click(object sender, EventArgs e)
    {
        if (ImageExtractor == null)
        {
            MessageBox.Show("イメージが開かれていません");
            return;
        }

        var files = GetFiles();
        if (!files.Any())
        {
            MessageBox.Show("イメージにファイルがありません。");
            return;
        }

        var result = folderBrowserDialog1.ShowDialog(this);
        if (result == DialogResult.Cancel) return;

        try
        {
            var res = await StartExtract([.. files]);
            if (res != null)
            {
                ShowExtractionResult(res);
            }
            else
            {
                MessageBox.Show($"エラーが発生しました");
            }

        }
        catch (Exception ex)
        {
            MessageBox.Show($"エラー: {ex.Message}");
        }
    }

    private async void Extract_Click(object sender, EventArgs e)
    {
        if (ImageExtractor == null)
        {
            MessageBox.Show("イメージが開かれていません");
            return;
        }

        var files = GetFiles();
        if (!files.Any())
        {
            MessageBox.Show("イメージにファイルがありません。");
            return;
        }

        var checkedFiles = listViewFiles.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag as FatFileEntry)
            .Where(file => file != null)
            .ToList();

        if (checkedFiles.Count == 0)
        {
            MessageBox.Show("ファイルが選択されていません。");
            return;
        }

        var result = folderBrowserDialog1.ShowDialog(this);
        if (result == DialogResult.Cancel) return;

        try
        {
            var res = await StartExtract(checkedFiles!);
            if (res != null)
            {
                ShowExtractionResult(res);
            }
            else
            {
                MessageBox.Show($"エラーが発生しました");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エラー: {ex.Message}");
        }
    }

    async Task<ExtractReport?> StartExtract(List<FatFileEntry> fileList)
    {
        if (ImageExtractor == null)
        {
            throw new InvalidOperationException("イメージが開かれていません");
        }

        try
        {
            CancellationTokenSource = new CancellationTokenSource();

            using var formProgress = CreateProgressForm(fileList.Count, () => CancellationTokenSource.Cancel());
            formProgress.Show(this);

            var extractorService = new FileExtractorService(ImageExtractor);
            var extractTask = extractorService.ExtractFilesAsync(
                fileList,
                folderBrowserDialog1.SelectedPath,
                CancellationTokenSource.Token,
                formProgress.GetReporter());
            ExtractTask = extractTask;
            var result = await extractTask;

            //ShowExtractionResult(result);
            return result;
        }
        catch (Exception)
        {
            // ユーザーには例外メッセージのみを表示する方が親切
            //MessageBox.Show($"エラー: {ex.Message}");
            return null;
        }
        finally
        {
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = null;
            ExtractTask = null;
        }
    }

    private FormProgress CreateProgressForm(int MaxCount, Action cancelAction)
    {
        FormProgress formProgress = new()
        {
            MaxCount = MaxCount,
            Value = 0,
            Title = "ファイルを抽出しています",
            Opacity = 0
        };

        formProgress.CancelClicked += cancelAction;

        formProgress.Shown += (s, e) =>
        {
            formProgress.Left = this.Left + (this.Width - formProgress.Width) / 2;
            formProgress.Top = this.Top + (this.Height - formProgress.Height) / 2;
            formProgress.Opacity = 1;
        };

        return formProgress;
    }

    private static void ShowExtractionResult(ExtractReport result)
    {
        var canceledMessage = result.IsCanceled ? "キャンセルしました。" : "";
        var successMessage = $"{result.SuccessCount}個のファイルを抽出しました。";
        var errorMessage = result.ErrorCount > 0 ? $"{result.ErrorCount}件のエラーがありました。" : "";

        var message = $"{canceledMessage}{successMessage}{errorMessage}";

        MessageBox.Show(message);
    }

    private async void FormDiskImageTool_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (ExtractTask != null)
        {
            var res = MessageBox.Show("ファイルを抽出中です。終了してよろしいですか?", "終了", MessageBoxButtons.OKCancel);
            if (res == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (CancellationTokenSource != null)
            {
                Debug.WriteLine("\ncancelling task");
                CancellationTokenSource.Cancel();
            }

            // UIスレッドをブロックしないように非同期で待機
            if (ExtractTask != null && !ExtractTask.IsCompleted)
            {
                Debug.WriteLine("\nawaiting task");
                await ExtractTask;
            }
        }
    }

    private void buttonFATInfo_Click(object sender, EventArgs e)
    {
        if (ImageExtractor?.FileSystem == null)
        {
            MessageBox.Show("イメージが開かれていません");
            return;
        }

        FormFatInfo formInfo = new FormFatInfo(ImageExtractor.FileSystem);
        formInfo.ShowDialog(this);
    }

    private void listViewFiles_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        SortOrder = e.Column switch
        {
            1 => SortOrder.Size, //size
            2 => SortOrder.Date, //date
            _ => SortOrder.Name, //name
        };

        SortDirection = SortOrder != lastSortOrder
            ? SortDirection.Ascending
            : SortDirection == SortDirection.Descending ? SortDirection.Ascending : SortDirection.Descending;
        SortColumn = e.Column;
        lastSortOrder = SortOrder;

        var files = GetFiles();
        UpdateListView(files);
    }

    private void checkIsUTC_Click(object sender, EventArgs e)
    {
        if (ImageExtractor == null)
        {
            return;
        }

        if (ImageFile.Length == 0 || ImageFormat == ImageFormat.Unknown) return;

        var files = GetFiles();
        UpdateListView(files);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        ImageExtractor?.Dispose();
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
        if (e.ColumnIndex == SortColumn)
        {
            DrawSortTriangle(e.Bounds, e.Graphics);
        }
    }

    void DrawSortTriangle(Rectangle bounds, Graphics g)
    {
        var x = bounds.X + bounds.Width - TRIANGLE_OFFSET;
        var y = bounds.Y + bounds.Height / 2;
        var pts = new List<Point>
            {
                new Point(x, y + TRIANGLE_SIZE / 2 * (SortDirection == SortDirection.Ascending ? -1 : 1)),
                new Point(x - TRIANGLE_SIZE / 2, y - TRIANGLE_SIZE / 2 * (SortDirection == SortDirection.Ascending ? -1 : 1)),
                new Point(x + TRIANGLE_SIZE / 2, y - TRIANGLE_SIZE / 2 * (SortDirection == SortDirection.Ascending ? -1 : 1))
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
