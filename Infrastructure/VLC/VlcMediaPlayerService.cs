using System;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using IntuitiveMedia.Core;
using CoreState = IntuitiveMedia.Core.PlaybackState;

namespace IntuitiveMedia.Infrastructure.VLC;

public sealed class VlcMediaPlayerService : IMediaPlayerService
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _currentMedia;
    private bool _disposed;

    public VlcMediaPlayerService(VlcCoreInitializer coreInitializer)
    {
        ArgumentNullException.ThrowIfNull(coreInitializer);

        _libVlc = coreInitializer.LibVlc;
        _mediaPlayer = new MediaPlayer(_libVlc);

        // Todos os handlers abaixo rodam em thread nativa do libVLC.
        // Cada um só faz o trabalho mínimo e despacha para a UI thread
        // usando Post (assíncrono) — nunca Invoke síncrono, para evitar deadlock.
        _mediaPlayer.Playing += (_, _) => RaiseStateChanged(CoreState.Playing);
        _mediaPlayer.Paused += (_, _) => RaiseStateChanged(CoreState.Paused);
        _mediaPlayer.Stopped += (_, _) => RaiseStateChanged(CoreState.Stopped);
        _mediaPlayer.EndReached += (_, _) => RaiseStateChanged(CoreState.Ended);
        _mediaPlayer.Opening += (_, _) => RaiseStateChanged(CoreState.Opening);

        _mediaPlayer.EncounteredError += (_, _) =>
        {
            RaiseStateChanged(CoreState.Error);
            Dispatcher.UIThread.Post(() =>
                ErrorOccurred?.Invoke(this, "O libVLC encontrou um erro durante a reprodução."));
        };

        _mediaPlayer.TimeChanged += (_, e) =>
        {
            var position = TimeSpan.FromMilliseconds(e.Time);
            Dispatcher.UIThread.Post(() => PositionChanged?.Invoke(this, position));
        };

        _mediaPlayer.LengthChanged += (_, e) =>
        {
            var duration = TimeSpan.FromMilliseconds(Math.Max(0, e.Length));
            Dispatcher.UIThread.Post(() => DurationChanged?.Invoke(this, duration));
        };
    }

    public CoreState State { get; private set; } = CoreState.Stopped;

    public TimeSpan Position => TimeSpan.FromMilliseconds(_mediaPlayer.Time);

    public TimeSpan Duration => TimeSpan.FromMilliseconds(_mediaPlayer.Length);
    public double Volume => _mediaPlayer.Volume; // #### TESTE ####

    /// <summary>
    /// Handle opaco (na prática, o LibVLCSharp.Shared.MediaPlayer) exposto
    /// só para a View poder fazer VideoView.MediaPlayer = handle as MediaPlayer.
    /// </summary>
    public object? NativePlayerHandle => _mediaPlayer;

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<TimeSpan>? DurationChanged;
    public event EventHandler<string>? ErrorOccurred;

    public event EventHandler<double>? VolumeChanged;


    public void Play(Uri source)
    {
        // Descarta a mídia anterior antes de trocar — evita vazamento de handles nativos.
        var previousMedia = _currentMedia;

        _currentMedia = new Media(_libVlc, source);
        _mediaPlayer.Play(_currentMedia);

        previousMedia?.Dispose();
    }

    public void Pause() => _mediaPlayer.Pause();

    public void Resume()
    {
        if (!_mediaPlayer.IsPlaying)
            _mediaPlayer.Play();
    }

    public void Stop() => _mediaPlayer.Stop();

    public void Seek(TimeSpan position) => _mediaPlayer.Time = (long)position.TotalMilliseconds;

    public void SetVolume(double volume)
    {
        var normalizedVolume = Math.Clamp((int)volume, 0, 100);
        _mediaPlayer.Volume = normalizedVolume;

        if (Dispatcher.UIThread.CheckAccess())
            VolumeChanged?.Invoke(this, normalizedVolume);
        else
            Dispatcher.UIThread.Post(() => VolumeChanged?.Invoke(this, normalizedVolume));
    }

    private void RaiseStateChanged(CoreState newState)
    {
        var oldState = State;
        State = newState;

        Dispatcher.UIThread.Post(() =>
            StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(oldState, newState)));
    }

    /// <summary>
    /// Ordem de Dispose deliberada e obrigatória:
    /// Stop -> Media -> MediaPlayer. O LibVLC (singleton) é descartado à parte,
    /// pelo VlcCoreInitializer, só quando a aplicação inteira está encerrando.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _mediaPlayer.Stop();
        _currentMedia?.Dispose();
        _mediaPlayer.Dispose();
    }

}
