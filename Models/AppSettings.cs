using System;
using System.Collections.Generic;

public enum RepeatMode { None, One, All }

public class AppSettings
{
    public int Version { get; set; } = 1;
    public Preferences Preferences { get; set; } = new();
    public Guid? CurrentPlaylistId { get; set; }
    public List<PlaylistData> Playlists { get; set; } = new();
}

public class Preferences
{
    public bool Shuffle { get; set; }
    public RepeatMode RepeatMode { get; set; } = RepeatMode.None;
    public bool Autoplay { get; set; }
    public int Volume { get; set; } = 80;
    public bool Muted { get; set; }
}

public class PlaylistData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public List<ItemData> Items { get; set; } = new();
    public Guid? CurrentItemId { get; set; }
    public long PositionMs { get; set; }
}

public class ItemData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Path { get; set; } = "";
    public string Title { get; set; } = "";
}