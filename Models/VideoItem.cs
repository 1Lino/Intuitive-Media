namespace IntuitiveMedia.Models;

using Avalonia.Media.Imaging;

public class VideoItem
{
    public string Title { get; set; } = "";
    public string Path { get; set; } = "";
    public Bitmap? Thumbnail { get; set; }   // isso pode ser null enquanto não gerar a miniatura do vídeo
}