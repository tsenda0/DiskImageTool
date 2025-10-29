namespace DiskImageTool
{
    public partial class FormFatInfo : Form
    {
        public FormFatInfo(FatFileSystem fat)
        {
            InitializeComponent();
            propertyGrid1.SelectedObject = fat;
        }
    }
}
