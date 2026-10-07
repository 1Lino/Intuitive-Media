namespace IntuitiveMedia.Models;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

public class VideoItem : INotifyPropertyChanged
{
    public string Title { get; set; } = "";
    public string Path { get; set; } = "";
    private bool _isCurrent;
    private Bitmap? _thumbnail;   // isso pode ser null enquanto não gerar a miniatura do vídeo

    public bool IsCurrent
    {
        get => _isCurrent;
        internal set
        {
            if (_isCurrent == value)
                return;

            _isCurrent = value;
            OnPropertyChanged();
        }
    }

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        set
        {
            if (_thumbnail == value)
                return;

            _thumbnail = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}