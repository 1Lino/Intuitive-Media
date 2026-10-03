using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using IntuitiveMedia.Core;
using IntuitiveMedia.Infrastructure.VLC;
using IntuitiveMedia.Models;

namespace IntuitiveMedia.ViewModels;

/// ViewModel do player. Nenhuma referência a LibVLCSharp aqui,
/// só à abstração IMediaPlayerService. Todos os eventos consumidos abaixo
/// já chegam despachados na UI thread pela implementação concreta.
public sealed partial class PlayerViewModel : ViewModelBase, IDisposable
{
    // ObservableCollection torna viável o sistema de reposicionamento de vídeos na playlist por meio de draging
    public ObservableCollection<VideoItem> Playlist { get; } = new();

    // Ao invés de [ObservableProperty], deve-se usar essa estrutura aqui, que segue INotifyPropertyChanged
    // do ViewModelBase, que basicamente faz o serviço de notificar ao binding da UI quando estas propriedades são mudadas.
    // O que fica exposto para a UI é o AreControlsVisible, por exemplo, enquanto que _areControlsVisible é interno. Só não funciona com listas, pois SetField não foi criado levando em conta listas, apenas valores individuais.
    private readonly IMediaPlayerService _player;
    private bool _autoRepeat = false; // TODO: tem que implementar as mecânicas envolvendo autoRepeat e plalistend.
    private bool _isPlaylistEnd = false;
    private VideoItem _currentFile;
    private PlaybackState _state;
    private TimeSpan _position;
    private TimeSpan _duration;
    private int _volume = 80;
    private string? _lastError;
    private bool _isPointerOverControls;
    private bool _areControlsVisible;
    private bool _isVideoDrawerOn;

    // Adiante tudo os que ficará exposto no para o resto do projeto:
    public PlayerViewModel(IMediaPlayerService player)
    {
        _player = player;
        _player.StateChanged += OnStateChanged;
        _player.PositionChanged += OnPositionChanged;
        _player.DurationChanged += OnDurationChanged;
        _player.ErrorOccurred += OnErrorOccurred;
        _player.VolumeChanged += OnVolumeChanged;
        _player.SetVolume(_volume);
    }

    public bool AutoRepeat
    {
        get => _autoRepeat;
        set => SetField(ref _autoRepeat, value);
    }

    public bool IsPlaylistEnd
    {
        get => _isPlaylistEnd;
        set => SetField(ref _isPlaylistEnd, value);
    }

    public VideoItem CurrentFile
    {
        get => _currentFile;
        set => SetField(ref _currentFile, value);
    }

    public bool IsPointerOverControls
    {
        get => _isPointerOverControls;
        set => SetField(ref _isPointerOverControls, value);
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
        private set
        {
            if (SetField(ref _position, value))
                OnPropertyChanged(nameof(PositionSeconds));
        }
    }

    public double PositionSeconds
    {
        get => _position.TotalSeconds;
        set => Seek(TimeSpan.FromSeconds(value));
    }

    public TimeSpan Duration
    {
        get => _duration;
        private set
        {
            if (SetField(ref _duration, value))
                OnPropertyChanged(nameof(DurationSeconds));
        }
    }

    public double DurationSeconds => _duration.TotalSeconds;

    public int Volume
    {
        get => _volume;
        set
        {
            var volume = Math.Clamp(value, 0, 100);
            if (SetField(ref _volume, volume))
                _player.SetVolume(volume);
        }
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

    public void Play(VideoItem video)
    {
        Console.WriteLine(video != null);
        Position = TimeSpan.Zero;
        Duration = TimeSpan.Zero;
        _player.Play(new Uri(video.Path));
    }

    public void Pause() => _player.Pause();

    public void Resume() => _player.Resume();

    public void Stop() => _player.Stop();

    public void Seek(TimeSpan position)
    {
        var clampedPosition = TimeSpan.FromMilliseconds(
            Math.Clamp(position.TotalMilliseconds, 0, Duration.TotalMilliseconds));
        Position = clampedPosition;
        _player.Seek(clampedPosition);
    }

    private void OnStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        if (e.NewState == PlaybackState.Ended)
        {
            ReachedEndOfPlay();
        }

        State = e.NewState;
    }

    private void OnPositionChanged(object? sender, TimeSpan position) => Position = position;

    private void OnDurationChanged(object? sender, TimeSpan duration) => Duration = duration;

    private void OnVolumeChanged(object? sender, double volume) =>
        SetField(ref _volume, Math.Clamp((int)volume, 0, 100), nameof(Volume));

    private void OnErrorOccurred(object? sender, string message) => LastError = message;

    private void ReachedEndOfPlay()
    {
        if (IsPlaylistEnd)
        {
            if (AutoRepeat)
                Play(CurrentFile);
            else
                Stop();
        }
        else
        {
            if (AutoRepeat)
            {
                Play(CurrentFile);
            }
            else
            {
                int currentVideoIndex = Playlist.IndexOf(CurrentFile);
                int playlistLastIndex = Playlist.IndexOf(Playlist.Last());

                // Console.WriteLine($"\nVideo que acabou de tocar: {CurrentFile.Title}\nVideo Index: {currentVideoIndex}");

                // Analisa se o índice para o próximo vídeo é menor ou igual ao índice do último vídeo da playlist
                // se sim, segue com a incrementação. Se não, o índice do próximo vídeo não atualiza.
                int nextVideoIndex = currentVideoIndex + 1 <= playlistLastIndex ? currentVideoIndex + 1 : currentVideoIndex;

                VideoItem nextVideo = Playlist[nextVideoIndex];

                // Console.WriteLine($"Video pra tocar agora: {nextVideo.Title}\nVideo Index: {nextVideoIndex}");

                Play(nextVideo);

                // isto aqui é importante, do contrário "CurrentFile" será sempre o vídeo de índice 0.
                CurrentFile = nextVideo;

                // checagem básica de fim de playlist:
                if (nextVideoIndex == playlistLastIndex)
                {
                    IsPlaylistEnd = true;
                }
            }

        }
    }

    public void Dispose()
    {
        _player.StateChanged -= OnStateChanged;
        _player.PositionChanged -= OnPositionChanged;
        _player.DurationChanged -= OnDurationChanged;
        _player.ErrorOccurred -= OnErrorOccurred;
        _player.VolumeChanged -= OnVolumeChanged;

        _player.Dispose();
    }
}
