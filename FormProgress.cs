namespace DiskImageTool;

public partial class FormProgress : Form
{
    public event Action? CancelClicked;

    Progress<ExtractReport>? reporter; // = new Progress<ExtractReport>(Report);

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
        buttonCancel.Enabled = false;
        CancelClicked?.Invoke();
    }

    void report(ExtractReport report)
    {
        if (IsDisposed) return;

        if (report.SuccessCount.HasValue) Value = report.SuccessCount.Value;

        if (report.SuccessCount.HasValue && report.TotalCount.HasValue && report.CompletedBytes.HasValue && report.TotalBytes.HasValue)
        {
            var cur = $"{report.SuccessCount.Value:N0}";
            var total = $"{report.TotalCount.Value:N0}";
            var compBytes = $"{report.CompletedBytes.Value:N0}";
            var totalBytes = $"{report.TotalBytes.Value:N0}";
            Message = $"({cur} / {total}) ({compBytes} / {totalBytes} bytes)";
        }

        if (report.CurrentFileName != null && report.CurrentFileLength.HasValue)
        {
            var curLen = $" ({report.CurrentFileLength:N0} bytes)";
            CurrentFile = report.IsCanceled ? "キャンセルしています" : $"{report.CurrentFileName}{curLen}";
        }
    }

    public Progress<ExtractReport> GetReporter()
    {
        reporter ??= new Progress<ExtractReport>(report);
        return reporter;
    }
}
