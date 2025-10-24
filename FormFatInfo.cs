namespace DiskImageTool
{
    public partial class FormFatInfo : Form
    {
        public FormFatInfo(FatFileSystem fat)
        {
            InitializeComponent();
            this.propertyGrid1.SelectedObject = fat;
        }
    }
}
