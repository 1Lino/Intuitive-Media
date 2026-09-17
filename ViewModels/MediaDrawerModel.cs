// Para instalar lib do VLC pro Avalonia: dotnet add package LibVLCSharp && dotnet add package LibVLCSharp.Avalonia
// E para que o VLC funcione, também: dotnet add package VideoLAN.LibVLC.Windows (pois do contrário teria de baixar o app da VLC e puxar o diretório das dlls do app para Core.Initialize)
using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp.Shared;

namespace IntuitiveMedia.ViewModels;

// TODO: O drawer que contém os videos deve ser um componente separado com seu próprio Data Context.
// Esse Data context deve ser compartilhado também com o componente de reprodução de mídia PlayerView.
public partial class MediaDrawerModel : ViewModelBase
{
    [ObservableProperty]
    public partial bool AreControlsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsPointerOverControls { get; set; }

    [ObservableProperty]
    public partial bool IsVideoDrawerOn { get; set; }

    public LibVLC LibVLC { get; }
    public MediaPlayer MediaPlayer { get; }

    public MediaDrawerModel()
    {
        Core.Initialize();

        LibVLC = new LibVLC();
        MediaPlayer = new MediaPlayer(LibVLC);
    }
}
