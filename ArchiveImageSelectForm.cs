namespace DiskImageTool;

public partial class ArchiveImageSelectForm : Form
{
    public ArchiveImageSelectForm()
    {
        InitializeComponent();
    }

    IEnumerable<string> files = [];

    public IEnumerable<string> Files
    {
        get => Files;
        set
        {
            files = value;
            listFiles.DataSource = files.ToList();
        }
    }

    public string? SelectedFile => listFiles.SelectedItem as string;
}
