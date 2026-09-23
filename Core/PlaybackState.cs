namespace IntuitiveMedia.Core;

/// <summary>
/// Estado de reprodução em vocabulário da aplicação — não é o enum do libVLC.
/// Mantemos o nosso próprio para não acoplar o resto do app ao LibVLCSharp.
/// </summary>
public enum PlaybackState
{
    Stopped,
    Opening,
    Playing,
    Paused,
    Ended,
    Error
}
