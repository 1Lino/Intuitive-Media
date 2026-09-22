// Para instalar lib do VLC pro Avalonia: dotnet add package LibVLCSharp && dotnet add package LibVLCSharp.Avalonia
// E para que o VLC funcione, também: dotnet add package VideoLAN.LibVLC.Windows (Este último comando é necessário porque .NET precisa dos binários nativos do VLC.)
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp.Shared;

namespace IntuitiveMedia.ViewModels;

// TODO: já que restruturei algumas coisas para melhor integração entre lib do VLC e o Avalonia, esta classe MediaDrawerModel deve ter seu nome mudado, uma vez que já não trata apenas de um "drawer/gaveta" de vídeo.
public partial class MediaDrawerModel : ViewModelBase, IDisposable
{
    [ObservableProperty]
    public partial bool AreControlsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsPointerOverControls { get; set; }

    [ObservableProperty]
    public partial bool IsVideoDrawerOn { get; set; }


    private bool _disposed;
    private Media? _loadedMedia;
    public LibVLC LibVLC { get; }
    public MediaPlayer MediaPlayer { get; }

    // importante inicializar _playlist usando new(), para evitar NullReferenceException ao puxar os endereços das mídias para cá.
    public List<string> _playlist { get; set; } = new();

    public string currentMedia { get; set; } = string.Empty;

    public bool isPlaylistEnd { get; set; } = true; // isto deve controlar se a playlist está ou não no final. Por causa que o EndReached do MediaPlayer deve depender disso.
    public bool autoRepeat { get; set; } = false; // com essa propriedade como "true", o currentMedia deverá ser reproduzido repedidamente automaticamente.

    public MediaDrawerModel()
    {
        Core.Initialize();

        LibVLC = new LibVLC();
        MediaPlayer = new MediaPlayer(LibVLC);

        MediaPlayer.EndReached += (_, _) => ReachedEndOfPlay();

    }

    public bool PlayMedia(string path)
    {
        var media = new Media(LibVLC, new Uri(path));
        if (!MediaPlayer.Play(media))
        {
            media.Dispose();
            return false;
        }

        _loadedMedia?.Dispose();
        _loadedMedia = media;
        currentMedia = path;
        return true;
    }

    // Este método deve oferecer três possibilidades, quando um vídeo terminar de ser executado: se o vídeo tiver numa playlist, o próximo vídeo da lista deve começar a ser executado; se a opção "repeat" estiver ligada, deve repetir o mesmo vídeo automaticamente; e caso o vídeo esteja no fim da playlist e não tenha "repeat" ligado, ele deve apenas ficar precarregado para que o usuário possa executá-lo novamente. O callback do Dispatcher deve levar em consideração as possibilidades supracitadas.
    private void ReachedEndOfPlay()
    {
        // se o vídeo estiver no final da playlist e com autoRepeat desativado.
        if (isPlaylistEnd && !autoRepeat)
        {
            // O callback vem de uma thread do VLC. Adiar a operação evita
            // reentrar no ciclo interno de reprodução enquanto ele termina.
            Dispatcher.UIThread.Post(() => MediaPlayer.Stop());
        }
    }

    // Esse método aqui é importante para que LibVLC e MediaPlayer sejam finalizados na memória, já que o Garbage Collector do .NET não gerencia tais objetos, uma vez que são de bibliotecas feitas em cima de C (o sistema até limpa tais objetos da memória por conta própria, mas não imediatamente, então convém que, toda vez que o app seja finalizado, chamar este método Dispose em App.axaml.cs pra fazer essa limpeza manual imediata).
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        MediaPlayer.Stop();
        _loadedMedia?.Dispose();
        MediaPlayer.Dispose();
        LibVLC.Dispose();

        GC.SuppressFinalize(this);
    }
}
