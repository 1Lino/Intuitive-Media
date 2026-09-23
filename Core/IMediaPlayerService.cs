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

    /// <summary>
    /// Handle opaco para o player nativo (ex: LibVLCSharp.Shared.MediaPlayer).
    /// Só a camada de View (que já referencia LibVLCSharp.Avalonia para hospedar
    /// o VideoView) deve fazer cast disso. ViewModels nunca devem tocar aqui.
    /// </summary>
    object? NativePlayerHandle { get; }

    event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    /// <summary>Disparado já na UI thread — seguro para bind direto.</summary>
    event EventHandler<TimeSpan>? PositionChanged;

    /// <summary>Disparado já na UI thread — seguro para bind direto.</summary>
    event EventHandler<string>? ErrorOccurred;

    void Play(Uri source);

    void Pause();

    void Resume();

    void Stop();

    void Seek(TimeSpan position);
}
