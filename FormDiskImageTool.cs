using System.Diagnostics;

namespace DiskImageTool;

public partial class FormDiskImageTool : Form
{
    IImageExtractor? ImageExtractor;
    IEnumerable<FatFile> Files = [];

    CancellationTokenSource? CancellationTokenSource;
    Task<(int, int)>? ExtractTask;

    public FormDiskImageTool()
    {
        InitializeComponent();
        InitFileDialog();
    }

    void InitFileDialog()
    {
        openFileDialog1.CheckFileExists = true;
        openFileDialog1.Multiselect = false;
        openFileDialog1.CheckPathExists = true;
        openFileDialog1.Filter = @"DCUイメージファイル(*.DCU)|*.DCU|rawイメージファイル(*.IMG)|*.IMG|すべてのファイル(*.*)|*.*";
        openFileDialog1.FilterIndex = 0;
        openFileDialog1.RestoreDirectory = true;
        openFileDialog1.FileName = "";
    }

    private void OpenImageFile_Click(object sender, EventArgs e)
    {
        var result = openFileDialog1.ShowDialog(this);
        if (result == DialogResult.Cancel) return;

        ImageExtractor = null;
        this.Files = [];

        labelFileName.Text = "";
        listViewFiles.Items.Clear();

        try
        {
            switch (openFileDialog1.FilterIndex)
            {
                case 1:
                    ImageExtractor = new DcuExtractor();
                    this.Files = ImageExtractor.OpenImage(openFileDialog1.FileName).OrderBy(f => f.Name);
                    break;
                default:
                    ImageExtractor = new RawExtractor();
                    this.Files = ImageExtractor.OpenImage(openFileDialog1.FileName).OrderBy(f => f.Name);
                    break;
            }
            labelFileName.Text = openFileDialog1.FileName;

            if (ImageExtractor?.FileSystem != null)
            {
                Debug.WriteLine($"FAT type: {ImageExtractor.FileSystem.FatType}");
                Debug.WriteLine($"bytes per sector: {ImageExtractor.FileSystem.BytesPerSector}");
                Debug.WriteLine($"sectors per cluster: {ImageExtractor.FileSystem.SectorsPerCluster}");
                Debug.WriteLine($"reserved sectors count: {ImageExtractor.FileSystem.ReservedSectorCount}");
                Debug.WriteLine($"total sectors count(16): {ImageExtractor.FileSystem.TotalSector16}");
                Debug.WriteLine($"total sectors count(32): {ImageExtractor.FileSystem.TotalSector32}");
                Debug.WriteLine($"number of FATs: {ImageExtractor.FileSystem.NumFats}");
                Debug.WriteLine($"FAT size(sector count): {ImageExtractor.FileSystem.FatSize16}");
                Debug.WriteLine($"root entries count: {ImageExtractor.FileSystem.RootEntriesCount}");

                Debug.WriteLine($"image size: {ImageExtractor.FileSystem.ImageSizeBytes}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エラー: {ex.Message}");

        }

        listViewFiles.Items.AddRange([.. Files.Select(f =>
        {
            var item = new ListViewItem(f.Name);
            item.SubItems.Add(new ListViewItem.ListViewSubItem(item, $"{f.Length:N0}"));
            item.SubItems.Add(new ListViewItem.ListViewSubItem(item, $"{f.WriteDateTime:yyyy-MM-dd HH:mm:ss}"));
            //item.SubItems.Add(f.Date);
            item.Tag = f;
            return item;
        })]);
        labelStatus.Text = $@"{this.Files.Count()}ファイル";
    }

    private void ExtractAll_Click(object sender, EventArgs e)
    {
        if (ImageExtractor == null || ImageExtractor.ImageFile.Length == 0)
        {
            MessageBox.Show(@"イメージファイルが選択されていません。");
            return;
        }

        if (!Files.Any())
        {
            MessageBox.Show("ファイルがありません。");
            return;
        }

        var result = folderBrowserDialog1.ShowDialog(this);
        if (result == DialogResult.Cancel) return;

        StartExtract(Files);
    }

    private void Extract_Click(object sender, EventArgs e)
    {
        if (ImageExtractor == null || ImageExtractor.ImageFile.Length == 0)
        {
            MessageBox.Show(@"イメージファイルが選択されていません。");
            return;
        }

        if (!Files.Any())
        {
            MessageBox.Show("ファイルがありません。");
            return;
        }

        List<FatFile> checkedFiles = [];
        for (int i = 0; i < listViewFiles.CheckedItems.Count; i++)
        {
            if (listViewFiles.CheckedItems[i].Tag is FatFile f) checkedFiles.Add(f);
        }

        if (checkedFiles.Count == 0)
        {
            MessageBox.Show("ファイルが選択されていません。");
            return;
        }

        var result = folderBrowserDialog1.ShowDialog(this);
        if (result == DialogResult.Cancel) return;

        StartExtract(checkedFiles);
    }

    async void StartExtract(IEnumerable<FatFile> files)
    {
        CancellationTokenSource = new CancellationTokenSource();

        using var formProgress = new FormProgress();
        formProgress.CancelClicked += () => CancellationTokenSource?.Cancel();
        formProgress.MaxCount = files.Count();
        formProgress.Value = 0;
        formProgress.Show(this);

        try
        {
            ExtractTask = ExtractFiles(files, CancellationTokenSource, formProgress);
            (int sucess, int error) = await ExtractTask;

            if (error == 0)
            {
                MessageBox.Show($"{sucess}個のファイルを抽出しました。");
            }
            else
            {
                MessageBox.Show($"{sucess}件のファイルを抽出し、{error}件のエラーがありました。");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エラー: {ex}");
        }
    }

    private Task<(int, int)> ExtractFiles(IEnumerable<FatFile> files, CancellationTokenSource tokenSource, FormProgress? formProgress)
    {
        return Task.Run(() =>
        {
            int success = 0;
            List<Exception> errors = [];
            var totalBytes = files.Sum(f => f.Length);
            uint nbytes = 0;

            Action<Action> invokeIfNeeded = action =>
            {
                if (formProgress == null) return;

                if (formProgress.InvokeRequired)
                    formProgress.BeginInvoke(action);
                else
                    action();
            };

            if (formProgress != null)
            {
                invokeIfNeeded(() =>
                {
                    formProgress.Title = "ファイルを抽出しています";
                });
            }

            foreach (var file in files)
            {
                if (tokenSource.IsCancellationRequested) break;

                try
                {
                    if (formProgress != null)
                    {
                        invokeIfNeeded(() =>
                        {
                            formProgress.Value++;
                            nbytes += file.Length;
                            formProgress.Message = $"({formProgress.Value:N0} / {formProgress.MaxCount:N0}) ({nbytes:N0} / {totalBytes:N0} bytes)";
                            formProgress.CurrentFile = $"{file.Name} ({file.Length:N0} bytes)";
                        });
                    }

                    ImageExtractor?.ExtractFile(file, folderBrowserDialog1.SelectedPath);
                    success++;
                }
                catch (Exception ex)
                {
                    //throw;
                    errors.Add(ex);
                }
            }

            return (success, errors.Count);
        }, tokenSource.Token);
    }

    private void FormDiskImageTool_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (CancellationTokenSource != null)
        {
            Debug.WriteLine("cancelling task"); CancellationTokenSource?.Cancel();
        }

        if (ExtractTask != null)
        {
            Debug.WriteLine("waiting task for stop"); ExtractTask?.Wait();
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
}
