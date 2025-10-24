using System.Diagnostics;

namespace DiskImageTool;

public partial class FormDiskImageTool : Form
{
    IImageExtractor? imageExtractor; // = new DcuExtractor();
    IEnumerable<FatFile> files = [];

    CancellationTokenSource? cts;
    Task<(int, int)>? extractTask;

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

        imageExtractor = null;
        this.files = [];

        labelFileName.Text = "";
        listViewFiles.Items.Clear();

        try
        {
            switch (openFileDialog1.FilterIndex)
            {
                case 1:
                    imageExtractor = new DcuExtractor();
                    this.files = imageExtractor.OpenImage(openFileDialog1.FileName).OrderBy(f => f.Name);
                    break;
                default:
                    imageExtractor = new RawExtractor();
                    this.files = imageExtractor.OpenImage(openFileDialog1.FileName).OrderBy(f => f.Name);
                    break;
            }
            labelFileName.Text = openFileDialog1.FileName;

            if (imageExtractor?.FileSystem != null)
            {
                Debug.WriteLine($"FAT type: {imageExtractor.FileSystem.FatType}");
                Debug.WriteLine($"bytes per sector: {imageExtractor.FileSystem.BytesPerSector}");
                Debug.WriteLine($"sectors per cluster: {imageExtractor.FileSystem.SectorsPerCluster}");
                Debug.WriteLine($"reserved sectors count: {imageExtractor.FileSystem.ReservedSectorCount}");
                Debug.WriteLine($"total sectors count(16): {imageExtractor.FileSystem.TotalSector16}");
                Debug.WriteLine($"total sectors count(32): {imageExtractor.FileSystem.TotalSector32}");
                Debug.WriteLine($"number of FATs: {imageExtractor.FileSystem.NumFats}");
                Debug.WriteLine($"FAT size(sector count): {imageExtractor.FileSystem.FatSize16}");
                Debug.WriteLine($"root entries count: {imageExtractor.FileSystem.RootEntriesCount}");

                Debug.WriteLine($"image size: {imageExtractor.FileSystem.ImageSizeBytes}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エラー: {ex.Message}");

        }

        listViewFiles.Items.AddRange([.. files.Select(f =>
        {
            var item = new ListViewItem(f.Name);
            item.SubItems.Add(new ListViewItem.ListViewSubItem(item, $"{f.Length:N0}"));
            item.SubItems.Add(new ListViewItem.ListViewSubItem(item, $"{f.WriteDateTime:yyyy-MM-dd HH:mm:ss}"));
            //item.SubItems.Add(f.Date);
            item.Tag = f;
            return item;
        })]);
        labelStatus.Text = $@"{this.files.Count()}ファイル";
    }

    private void ExtractAll_Click(object sender, EventArgs e)
    {
        if (imageExtractor == null || imageExtractor.ImageFile.Length == 0)
        {
            MessageBox.Show(@"イメージファイルが選択されていません。");
            return;
        }

        if (!files.Any())
        {
            MessageBox.Show("ファイルがありません。");
            return;
        }

        var result = folderBrowserDialog1.ShowDialog(this);
        if (result == DialogResult.Cancel) return;

        StartExtract(files);
    }

    private void Extract_Click(object sender, EventArgs e)
    {
        if (imageExtractor == null || imageExtractor.ImageFile.Length == 0)
        {
            MessageBox.Show(@"イメージファイルが選択されていません。");
            return;
        }

        if (!files.Any())
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
        cts = new CancellationTokenSource();

        using var formProgress = new FormProgress();
        formProgress.CancelClicked += () => cts?.Cancel();
        formProgress.MaxCount = files.Count();
        formProgress.Value = 0;
        formProgress.Show(this);

        try
        {
            extractTask = ExtractFiles(files, cts, formProgress);
            (int sucess, int error) = await extractTask;

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

                    imageExtractor?.ExtractFile(file, folderBrowserDialog1.SelectedPath);
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
        if (cts != null)
        {
            Debug.WriteLine("cancelling task"); cts?.Cancel();
        }

        if (extractTask != null)
        {
            Debug.WriteLine("waiting task for stop"); extractTask?.Wait();
        }
    }

    private void buttonFATInfo_Click(object sender, EventArgs e)
    {
        if (imageExtractor?.FileSystem == null)
        {
            MessageBox.Show("イメージが開かれていません");
            return;
        }

        FormFatInfo formInfo = new FormFatInfo(imageExtractor.FileSystem);
        formInfo.ShowDialog(this);
    }
}
