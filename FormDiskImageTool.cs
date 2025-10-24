namespace DiskImageTool;

public partial class FormDiskImageTool : Form
{
    IImageExtractor? imageExtractor; // = new DcuExtractor();
    IEnumerable<FatFile> files = [];

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

        List<Exception> errors = [];
        foreach (var file in files)
        {
            try
            {
                imageExtractor.ExtractFile(file, folderBrowserDialog1.SelectedPath);
            }
            catch (Exception ex)
            {
                //throw;
                errors.Add(ex);
            }
        }

        if (errors.Count == 0)
        {
            MessageBox.Show($"{files.Count() - errors.Count}個のファイルを抽出しました。");
        }
        else
        {
            MessageBox.Show($"{files.Count() - errors.Count}件のファイルを抽出し、{errors.Count}件のエラーがありました。");
        }
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

        if (listViewFiles.CheckedItems.Count == 0)
        {
            MessageBox.Show(@"ファイルが選択されていません。");
            return;
        }

        var result = folderBrowserDialog1.ShowDialog(this);
        if (result == DialogResult.Cancel) return;

        List<Exception> errors = [];
        for (int i = 0; i < listViewFiles.CheckedItems.Count; i++)
        {
            if (listViewFiles.CheckedItems[i].Tag is FatFile ent)
            {
                try
                {
                    imageExtractor.ExtractFile(ent, folderBrowserDialog1.SelectedPath);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }
        }

        if (errors.Count == 0)
        {
            MessageBox.Show($"{files.Count() - errors.Count}個のファイルを抽出しました。");
        }
        else
        {
            MessageBox.Show($"{files.Count() - errors.Count}件のファイルを抽出し、{errors.Count}件のエラーがありました。");
        }
    }

}
