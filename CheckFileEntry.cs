using System.ComponentModel;

namespace DiskImageTool;

public class CheckFileEntry(IFileEntry entry) : INotifyPropertyChanged
{
    //readonly IFileEntry entry = entry;

    bool isChecked;
    public bool Checked
    {
        get => isChecked; set
        {
            isChecked = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Checked)));
        }
    }

    public IFileEntry BaseEntry => entry;

    public event PropertyChangedEventHandler? PropertyChanged;
}
