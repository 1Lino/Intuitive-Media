using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using IntuitiveMedia.Core;

namespace IntuitiveMedia.ViewModels;

/// ViewModel do player. Nenhuma referência a LibVLCSharp aqui,
/// só à abstração IMediaPlayerService. Todos os eventos consumidos abaixo
/// já chegam despachados na UI thread pela implementação concreta.
public sealed partial class PlayerViewModel : ViewModelBase, IDisposable
{
    [ObservableProperty]
    public partial bool IsPointerOverControls { get; set; }

    public List<string> _playlist { get; set; } = new();

    public string currentMedia { get; set; } = string.Empty;

    public bool isPlaylistEnd { get; set; } = true; // isto deve controlar se a playlist está ou não no final. Por causa que o EndReached do MediaPlayer deve depender disso.
    public bool autoRepeat { get; set; } = false; // com essa propriedade como "true", o currentMedia deverá ser reproduzido repedidamente automaticamente.

    private readonly IMediaPlayerService _player;
    private PlaybackState _state;
    private TimeSpan _position;
    private string? _lastError;

    // Ao invés de [ObservableProperty], deve-se usar essa estrutura aqui, que segue INotifyPropertyChanged
    // do ViewModelBase, que basicamente faz o serviço de notificar ao binding da UI quando estas propriedades são mudadas.
    // O que fica exposto para a UI é o AreControlsVisible, por exemplo, enquanto que _areControlsVisible é interno. O ideal é que as variáveis públicas acima sigam lógica parecida, se possível
    private bool _areControlsVisible;
    private bool _isVideoDrawerOn;

    // Adiante tudo os que ficará exposto no para o resto do projeto:
    public PlayerViewModel(IMediaPlayerService player)
    {
        _player = player;
        _player.StateChanged += OnStateChanged;
        _player.PositionChanged += OnPositionChanged;
        _player.ErrorOccurred += OnErrorOccurred;
    }

    public bool AreControlsVisible
    {
        get => _areControlsVisible;
        set => SetField(ref _areControlsVisible, value);
    }

    public bool IsVideoDrawerOn
    {
        get => _isVideoDrawerOn;
        set => SetField(ref _isVideoDrawerOn, value);
    }

    public PlaybackState State
    {
        get => _state;
        private set => SetField(ref _state, value);
    }

    public TimeSpan Position
    {
        get => _position;
        private set => SetField(ref _position, value);
    }

    public string? LastError
    {
        get => _lastError;
        private set => SetField(ref _lastError, value);
    }

    // Exposto só para a camada de View conseguir fazer o "encaixe" nativo
    // no VideoView (VideoView.MediaPlayer = handle as MediaPlayer).
    // A View faz o cast; o ViewModel permanece agnóstico ao tipo real.
    public object? NativePlayerHandle => _player.NativePlayerHandle;

    // Começar a testar isto tudo:
    public void Play(Uri source) => _player.Play(source);

    public void Pause() => _player.Pause();

    public void Resume() => _player.Resume();

    public void Stop() => _player.Stop();

    public void Seek(TimeSpan position) => _player.Seek(position);

    private void OnStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        if (e.NewState == PlaybackState.Ended)
        {
            ReachedEndOfPlay();
        }

        State = e.NewState;
    }

    private void OnPositionChanged(object? sender, TimeSpan position) => Position = position;

    private void OnErrorOccurred(object? sender, string message) => LastError = message;

    private void ReachedEndOfPlay()
    {
        // se o vídeo estiver no final da playlist e com autoRepeat desativado.
        if (isPlaylistEnd && !autoRepeat)
        {
            // O callback vem de uma thread do VLC. Adiar a operação evita
            // reentrar no ciclo interno de reprodução enquanto ele termina.
            Stop();
        }
        else if (isPlaylistEnd && autoRepeat)
        {
            Console.WriteLine("Reached End of Play! Replaying...");
            Play(new Uri(currentMedia));
        }

        if (isPlaylistEnd)
        {
            if (autoRepeat)
                Play(new Uri(currentMedia));
            else
                Stop();
        }
        else
        {
            // TODO...
        }
    }

    public void Dispose()
    {
        _player.StateChanged -= OnStateChanged;
        _player.PositionChanged -= OnPositionChanged;
        _player.ErrorOccurred -= OnErrorOccurred;

        _player.Dispose();
    }
}
