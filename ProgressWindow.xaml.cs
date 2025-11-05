using System.Windows;

namespace DiskImageTool;
/// <summary>
/// ProgressWindow.xaml の相互作用ロジック
/// </summary>
public partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
    }

    public event Action<object, EventArgs>? Canceled;

    ExtractReport? report;
    public ExtractReport? Progress
    {
        get => report;
        set
        {
            report = value;
            if (report != null)
            {
                progressBar.Maximum = report.TotalCount ?? 1;
                progressBar.Value = report.SuccessCount ?? 0;
                labelTitle.Content = $"抽出しています {report.SuccessCount}/{report.TotalCount} ({report.CompletedBytes} / {report.TotalBytes} bytes)";
                labelMessage.Content = $"{report.CurrentFileName} ({report.CurrentFileLength} bytes)";
            }
            else
            {
                progressBar.Value = 0;
                labelTitle.Content = "";
                labelMessage.Content = "";
            }
        }
    }

    private void buttonCancel_Click(object sender, RoutedEventArgs e)
    {
        Canceled?.Invoke(this, EventArgs.Empty);
    }

    public IProgress<ExtractReport?> GetReporter()
    {
        return new Progress<ExtractReport?>((rep) => Progress = rep);
    }
}
