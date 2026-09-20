// Para instalar lib do VLC pro Avalonia: dotnet add package LibVLCSharp && dotnet add package LibVLCSharp.Avalonia
// E para que o VLC funcione, também: dotnet add package VideoLAN.LibVLC.Windows (Este último comando é necessário porque .NET precisa dos binários nativos do VLC.)
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public LibVLC LibVLC { get; }
    public MediaPlayer MediaPlayer { get; }

    // URL/endereço de vídeo de exemplo:
    private readonly string _url = "https://www.w3schools.com/html/mov_bbb.mp4";

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

    // Este comando deve abrir um diálogo para que o usuário selecione o vídeo que deseja executar no player. No caso, _url deve ser definida através deste método, e imediatamente dentro dele, o comando IniciarReproducao deve ser chamado, pois é o fluxo normal de uma ação de carregar mídia num player.
    [RelayCommand]
    private void CarregarVideo()
    {
        // string dialogueResponse = abrirDialogo();
        // _url = dialogueResponse;
        IniciarReproducao();
    }

    // O comando abaixo inicia a reprodução do vídeo quando clicar no controle associado a este (Binding).
    [RelayCommand]
    private void IniciarReproducao()
    {
        // "using" aqui é para que a mídia seja descartada da memória uma vez que seja removida do escopo, por exemplo, se o usuário trocar de vídeo, o vídeo anterior não fica na memória.
        using var media = new Media(LibVLC, new Uri(_url));
        MediaPlayer.Play(media);
    }

    [RelayCommand]
    private void PausarReproducao()
    {
        MediaPlayer.Pause();
    }
}
