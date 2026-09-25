using System;

namespace IntuitiveMedia.Core;

/// <summary>
/// Contrato de player de mídia consumido pelo resto da aplicação (ViewModels, etc).
/// Nenhuma implementação de LibVLCSharp deve vazar através desta interface.
/// </summary>
public interface IMediaPlayerService : IDisposable
{
    PlaybackState State { get; }

    TimeSpan Position { get; }

    TimeSpan Duration { get; }

    double Volume { get; } // #### TESTE ###

    /// <summary>
    /// Handle opaco para o player nativo (ex: LibVLCSharp.Shared.MediaPlayer).
    /// Só a camada de View (que já referencia LibVLCSharp.Avalonia para hospedar
    /// o VideoView) deve fazer cast disso. ViewModels nunca devem tocar aqui.
    /// </summary>
    object? NativePlayerHandle { get; }

    //StateChanged se refere a estes estados: Stopped,Opening,Playing,Paused,Ended,Error
    event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    event EventHandler<TimeSpan>? PositionChanged;

    event EventHandler<TimeSpan>? DurationChanged;

    event EventHandler<string>? ErrorOccurred;

    event EventHandler<double>? VolumeChanged; // #### TESTE ###

    void Play(Uri source);

    void Pause();

    void Resume();

    void Stop();

    void Seek(TimeSpan position);

    void SetVolume(double volume); // #### TESTE ###
}
