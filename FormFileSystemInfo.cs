namespace DiskImageTool;

public partial class FormFileSystemInfo : Form
{
    public FormFileSystemInfo(IFileSystem fat)
    {
        InitializeComponent();
        propertyGrid1.SelectedObject = fat;
    }
}
