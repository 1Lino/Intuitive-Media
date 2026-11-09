namespace IntuitiveMedia.Models;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;

public class VideoItem : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    private bool _isMissing;
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

    public bool IsMissing
    {
        get => _isMissing;
        set
        {
            if (_isMissing == value)
                return;

            _isMissing = value;
            OnPropertyChanged();
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}