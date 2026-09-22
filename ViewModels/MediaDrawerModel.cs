// Para instalar lib do VLC pro Avalonia: dotnet add package LibVLCSharp && dotnet add package LibVLCSharp.Avalonia
// E para que o VLC funcione, também: dotnet add package VideoLAN.LibVLC.Windows (Este último comando é necessário porque .NET precisa dos binários nativos do VLC.)
using System;
using System.Collections.Generic;
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

    // importante inicializar _files usando new(), para evitar NullReferenceException ao puxar os endereços das mídias para cá.
    public List<string> _files { get; set; } = new();

    private bool _disposed;
    public LibVLC LibVLC { get; }
    public MediaPlayer MediaPlayer { get; }

    public bool isMediaPaused { get; set; }
    public bool isWindowFullscreen { get; set; } = false;

    public MediaDrawerModel()
    {
        Core.Initialize();

        LibVLC = new LibVLC();
        MediaPlayer = new MediaPlayer(LibVLC);
    }

    // Esse método aqui é importante para que LibVLC e MediaPlayer sejam finalizados na memória, já que o Garbage Collector do .NET não gerencia tais objetos, uma vez que são de bibliotecas feitas em cima de C (o sistema até limpa tais objetos da memória por conta própria, mas não imediatamente, então convém que, toda vez que o app seja finalizado, chamar este método Dispose em App.axaml.cs pra fazer essa limpeza manual imediata).
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        MediaPlayer.Stop();
        MediaPlayer.Dispose();
        LibVLC.Dispose();

        GC.SuppressFinalize(this);
    }
}
