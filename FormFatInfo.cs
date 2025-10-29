namespace DiskImageTool;

public partial class FormFatInfo : Form
{
    public FormFatInfo(IFileSystem fat)
    {
        InitializeComponent();
        propertyGrid1.SelectedObject = fat;
    }
}
