namespace DiskImageTool
{
    public partial class FormProgress : Form
    {
        public event Action? CancelClicked;

        public FormProgress()
        {
            InitializeComponent();
            Title = "";
            Message = "";
            CurrentFile = "";
            MaxCount = 1;
            Value = 0;
        }

        public string Title
        {
            get => labelTitle.Text;
            set => labelTitle.Text = value;
        }

        public string Message
        {
            get => labelMessage.Text;
            set => labelMessage.Text = value;
        }

        public string CurrentFile
        {
            get => labelCurrent.Text;
            set => labelCurrent.Text = value;
        }

        public int MaxCount
        {
            get => progressBar1.Maximum;
            set => progressBar1.Maximum = value;
        }

        public int Value
        {
            get => progressBar1.Value;
            set => progressBar1.Value = value;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            CancelClicked?.Invoke();
        }
    }
}
