using System.Windows;

namespace DiskImageTool;

/// <summary>
/// ArchiveImageSelectForm.xaml の相互作用ロジック
/// </summary>
public partial class ArchiveImageSelectWindpw : Window, IFileSelectDialog
{
    public ArchiveImageSelectWindpw(Window? owner)
    {
        InitializeComponent();
        Owner = owner;

        listBox.MouseDoubleClick += listBox_MouseDoubleClick;
    }

    private void listBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (listBox.SelectedItem != null)
        {
            DialogResult = true;
            Close();
        }
    }

    //IEnumerable<string> Files { get; set; } = [];

    public string? SelectedFile { get; private set; } = "";

    public bool ShowDialog(IEnumerable<string> files)
    {
        listBox.ItemsSource = files;
        bool? res = ShowDialog();
        if (res == true)
        {
            SelectedFile = listBox.SelectedItem as string;
            return true;
        }

        return false;
    }

    private void button_Click(object sender, RoutedEventArgs e)
    {
        if (listBox.SelectedItem != null)
        {
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("ファイルを選択してください");
        }
    }

    private void button1_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
