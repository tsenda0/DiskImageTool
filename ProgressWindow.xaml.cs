using System.ComponentModel;
using System.Windows;

namespace DiskImageTool;
/// <summary>
/// ProgressWindow.xaml の相互作用ロジック
/// </summary>
public partial class ProgressWindow : Window
{
    public event Action<object, EventArgs>? Canceled;

    ExtractReport? report;
    bool closeFromCode;

    public ProgressWindow()
    {
        InitializeComponent();
        Closing += progressWindow_Closing;
    }

    public void CloseProgress()
    {
        closeFromCode = true;
        Close();
    }

    private void progressWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!closeFromCode) e.Cancel = true;
        Canceled?.Invoke(this, EventArgs.Empty);
    }

    public bool IsCancelEnabled
    {
        get => buttonCancel.IsEnabled;
        set => buttonCancel.IsEnabled = value;
    }

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
                labelTitle.Content = $"抽出しています {report.SuccessCount:N0}/{report.TotalCount:N0} ({report.CompletedBytes:N0} / {report.TotalBytes:N0} bytes)";
                labelMessage.Content = $"{report.CurrentFileName} ({report.CurrentFileLength:N0} bytes)";
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
