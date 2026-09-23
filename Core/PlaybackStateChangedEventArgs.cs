using System;

namespace IntuitiveMedia.Core;

public sealed class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackStateChangedEventArgs(PlaybackState oldState, PlaybackState newState)
    {
        OldState = oldState;
        NewState = newState;
    }

    public PlaybackState OldState { get; }

    public PlaybackState NewState { get; }
}
