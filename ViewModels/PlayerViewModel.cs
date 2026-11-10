using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using IntuitiveMedia.Core;
using IntuitiveMedia.Models;
using IntuitiveMedia.Services;

namespace IntuitiveMedia.ViewModels;

/// ViewModel do player. Nenhuma referência a LibVLCSharp aqui,
/// só à abstração IMediaPlayerService. Todos os eventos consumidos abaixo
/// já chegam despachados na UI thread pela implementação concreta.
public sealed partial class PlayerViewModel : ViewModelBase, IDisposable
{
    // ObservableCollection torna viável o sistema de reposicionamento de vídeos na playlist por meio de draging
    public ObservableCollection<VideoItem> Playlist { get; } = new(); // conjunto de vídeos

    // conjunto de playlists.
    public ObservableCollection<PlaylistItem> Playlists { get; } = new ObservableCollection<PlaylistItem>
    {
        // new PlaylistItem { Title = "Playlist 1"},
    };

    // Ao invés de [ObservableProperty], deve-se usar essa estrutura aqui, que segue INotifyPropertyChanged
    // do ViewModelBase, que basicamente faz o serviço de notificar ao binding da UI quando estas propriedades são mudadas.
    // O que fica exposto para a UI é o AreControlsVisible, por exemplo, enquanto que _areControlsVisible é interno. Só não funciona com listas, pois SetField não foi criado levando em conta listas, apenas valores individuais.
    private readonly IMediaPlayerService _player;
    private WindowState _windowStateBeforeFullScreen = WindowState.Normal;
    private int _playlistDrawerHeight = 200;
    private int _playlistDrawerWidth = 180;
    private int _mediaDrawerHeight = 150;
    private int _mediaDrawerWidth = 400;
    private int _playlistScrollHeight = 120;
    private bool _autoRepeat = false;
    private bool _autoplay = true;
    private bool _loop = false;
    private bool _shuffle = false;
    private bool _isMuted;
    private bool _isPlaylistEnd = false;
    private VideoItem? _currentFile;
    private PlaylistItem? _currentPlaylist;
    private Bitmap? _currentPlaylistThumbnail;
    private PlaybackState _state;
    private TimeSpan _position;
    private TimeSpan _duration;
    private int _volume = 80;
    private string? _lastError;
    private bool _isMouseDragging;
    private bool _isPointerOverControls;
    private bool _isMediaOptionsOn;
    private bool _areControlsVisible;
    private bool _isVideoDrawerOn;
    private bool _isPlaylistDrawerOn;
    private MediaOptions _mediaOptions = new MediaOptions
    {
        autoPlay = "✓ Autoplay",
        autoRepeat = "Repeat",
        loop = "Playlist Loop",
        shuffle = "Shuffle"
    };


    private AppSettings _settings = new();
    private PlaylistData? _current;
    private CancellationTokenSource? _saveCts;
    private bool _loading;
    private bool _preferencesInitialized;
    private bool _applyingPreferences;

    // Adiante tudo os que ficará exposto no para o resto do projeto:

    public PlayerViewModel(IMediaPlayerService player)
    {
        _player = player;
        _player.StateChanged += OnStateChanged;
        _player.PositionChanged += OnPositionChanged;
        _player.DurationChanged += OnDurationChanged;
        _player.ErrorOccurred += OnErrorOccurred;
        _player.VolumeChanged += OnVolumeChanged;
    }

    public ICommand ToggleAutoRepeatOption => new RelayCommand(() =>
    {
        UpdatePreferences(() =>
        {
            AutoRepeat = !AutoRepeat;
            AutoPlay = false;
            IsMediaOptionsOn = false;
        });
    });

    public ICommand ToggleAutoPlayOption => new RelayCommand(() =>
    {
        UpdatePreferences(() =>
        {
            AutoPlay = !AutoPlay;
            AutoRepeat = false;
            Shuffle = false;
            IsMediaOptionsOn = false;
        });
    });

    public ICommand ToggleLoopOption => new RelayCommand(() =>
    {
        UpdatePreferences(() =>
        {
            Loop = !Loop;
            Shuffle = false;
            IsMediaOptionsOn = false;
        });
    });

    public ICommand ToggleShuffleOption => new RelayCommand(() =>
    {
        UpdatePreferences(() =>
        {
            Shuffle = !Shuffle;
            Loop = false;
            AutoPlay = false;
            IsMediaOptionsOn = false;
        });
    });

    public WindowState WindowStateBeforeFullScreen
    {
        get => _windowStateBeforeFullScreen;
        set => SetField(ref _windowStateBeforeFullScreen, value);
    }

    public int PlaylistDrawerHeight
    {
        get => _playlistDrawerHeight;
        set => SetField(ref _playlistDrawerHeight, value);
    }

    public int PlaylistDrawerWidth
    {
        get => _playlistDrawerWidth;
        set => SetField(ref _playlistDrawerWidth, value);
    }

    public int PLaylistScrollHeight
    {
        get => _playlistScrollHeight;
        set => SetField(ref _playlistScrollHeight, value);
    }

    public int MediaDrawerHeight
    {
        get => _mediaDrawerHeight;
        set => SetField(ref _mediaDrawerHeight, value);
    }

    public int MediaDrawerWidth
    {
        get => _mediaDrawerWidth;
        set => SetField(ref _mediaDrawerWidth, value);
    }

    public MediaOptions MediaOptions
    {
        get => _mediaOptions;
        set => SetField(ref _mediaOptions, value);
    }

    public bool AutoRepeat
    {
        get => _autoRepeat;
        set
        {
            if (SetField(ref _autoRepeat, value))
                OnPreferencesChanged();
        }
    }

    public bool AutoPlay
    {
        get => _autoplay;
        set
        {
            if (SetField(ref _autoplay, value))
                OnPreferencesChanged();
        }
    }

    public bool Loop
    {
        get => _loop;
        set
        {
            if (SetField(ref _loop, value))
                OnPreferencesChanged();
        }
    }

    public bool Shuffle
    {
        get => _shuffle;
        set
        {
            if (SetField(ref _shuffle, value))
                OnPreferencesChanged();
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (SetField(ref _isMuted, value))
            {
                OnPropertyChanged(nameof(VolumeControlValue));
                if (!_applyingPreferences)
                    _player.SetVolume(_isMuted ? 0 : _volume);
                OnPreferencesChanged();
            }
        }
    }

    public bool IsPlaylistEnd
    {
        get => _isPlaylistEnd;
        set => SetField(ref _isPlaylistEnd, value);
    }

    public Bitmap? CurrentPlaylistThumbnail
    {
        get => _currentPlaylistThumbnail;
        set => SetField(ref _currentPlaylistThumbnail, value);
    }

    public VideoItem? CurrentFile
    {
        get => _currentFile;
        set
        {
            var previousFile = _currentFile;
            if (ReferenceEquals(previousFile, value))
                return;

            if (!SetField(ref _currentFile, value))
                return;

            // Atualiza a propriedade IsCurrent do vídeo anterior para "false"
            if (previousFile is not null)
                previousFile.IsCurrent = false;

            // e atualiza a mesma propriedade para o vídeo atual para "true"
            // de modo que tal propriedade possa ser lida pela interface, para
            // então aplicar comportamentos/estilos de acordo com estes valores.
            if (value is not null)
                value.IsCurrent = true;
        }
    }

    public PlaylistItem? CurrentPlaylist
    {
        get => _currentPlaylist;
        set
        {
            {
                var previousPlaylist = _currentPlaylist;
                if (ReferenceEquals(previousPlaylist, value))
                    return;

                if (!SetField(ref _currentPlaylist, value))
                    return;

                if (previousPlaylist is not null)
                    previousPlaylist.IsCurrent = false;

                if (value is not null)
                    value.IsCurrent = true;
            }
        }
    }

    public bool IsMouseDragging
    {
        get => _isMouseDragging;
        set => SetField(ref _isMouseDragging, value);
    }

    public bool IsMediaOptionsOn
    {
        get => _isMediaOptionsOn;
        set => SetField(ref _isMediaOptionsOn, value);
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
        set
        {
            if (SetField(ref _isVideoDrawerOn, value) && value && _current is not null)
                _ = LoadThumbnailsAsync(Playlist.Where(item => !item.IsMissing).ToList(), _current);
        }
    }

    public bool IsPlaylistDrawerOn
    {
        get => _isPlaylistDrawerOn;
        set
        {
            if (SetField(ref _isPlaylistDrawerOn, value) && value)
            {
                foreach (var playlistData in _settings.Playlists)
                    _ = LoadPlaylistThumbnailAsync(playlistData);
            }
        }
    }

    public PlaybackState State
    {
        get => _state;
        private set
        {
            if (SetField(ref _state, value))
                OnPropertyChanged(nameof(IsPaused));
        }
    }

    public bool IsPaused => State == PlaybackState.Paused;

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
            {
                OnPropertyChanged(nameof(VolumeControlValue));
                if (!_applyingPreferences)
                    _player.SetVolume(_isMuted ? 0 : volume);
                OnPreferencesChanged();
            }
        }
    }

    public int VolumeControlValue
    {
        get => IsMuted ? 0 : Volume;
        set
        {
            var volume = Math.Clamp(value, 0, 100);
            if (IsMuted)
            {
                if (volume > 0)
                {
                    Volume = volume;
                    IsMuted = false;
                }
                else
                {
                    OnPropertyChanged(nameof(VolumeControlValue));
                }

                return;
            }

            Volume = volume;
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

    public void Play(VideoItem? video)
    {
        if (video is not { } currentVideo)
            return;

        Position = TimeSpan.Zero;
        Duration = TimeSpan.Zero;
        _player.Play(new Uri(currentVideo.Path));
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

    public void Initialize(AppSettings settings, bool deferStartupWork = false)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
        _applyingPreferences = true;
        try
        {
            AutoPlay = _settings.Preferences.Autoplay;
            AutoRepeat = _settings.Preferences.RepeatMode == RepeatMode.One;
            Loop = _settings.Preferences.RepeatMode == RepeatMode.All;
            Shuffle = _settings.Preferences.Shuffle;
            IsMuted = _settings.Preferences.Muted;
            Volume = Math.Clamp(_settings.Preferences.Volume, 0, 100);
            UpdateMediaOptionLabels();
        }
        finally
        {
            _applyingPreferences = false;
            _preferencesInitialized = true;
        }
        _player.SetVolume(IsMuted ? 0 : Volume);

        var data = _settings.CurrentPlaylistId is { } currentPlaylistId
            ? _settings.Playlists.FirstOrDefault(p => p.Id == currentPlaylistId)
            : null;

        foreach (var playlistData in _settings.Playlists)
            AddOrUpdatePlaylistItem(playlistData);

        if (data is not null)
            LoadPlaylist(
                data,
                startPlayback: false,
                saveSettings: false,
                checkMissingFiles: !deferStartupWork,
                loadThumbnails: !deferStartupWork);

        Playlist.CollectionChanged += OnPlaylistChanged;   // Add, Move e Remove salvam sozinhos

        if (deferStartupWork)
        {
            if (data is not null)
                _ = ValidateStartupPlaylistAsync(data);
        }
    }

    private void OnPlaylistChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        if (!_loading) RequestSave();
    }

    private void OnPreferencesChanged()
    {
        if (_preferencesInitialized && !_applyingPreferences)
            RequestSave();
    }

    private void UpdatePreferences(Action update)
    {
        _applyingPreferences = true;
        try
        {
            update();
            UpdateMediaOptionLabels();
        }
        finally
        {
            _applyingPreferences = false;
        }

        OnPreferencesChanged();
    }

    private void UpdateMediaOptionLabels()
    {
        MediaOptions.autoPlay = AutoPlay ? "✓ Autoplay" : "Autoplay";
        MediaOptions.autoRepeat = AutoRepeat ? "✓ Repeat" : "Repeat";
        MediaOptions.loop = Loop ? "✓ Playlist Loop" : "Playlist Loop";
        MediaOptions.shuffle = Shuffle ? "✓ Shuffle" : "Shuffle";
    }

    private PlaylistItem AddOrUpdatePlaylistItem(PlaylistData data)
    {
        var playlistItem = Playlists.FirstOrDefault(item => item.Id == data.Id);
        if (playlistItem is null)
        {
            playlistItem = new PlaylistItem { Id = data.Id };
            Playlists.Add(playlistItem);
        }

        playlistItem.Title = data.Name;
        playlistItem.MediaPaths = data.Items.Select(item => item.Path).ToList();
        if (data.Thumbnail is not null)
            playlistItem.Thumbnail = data.Thumbnail;

        return playlistItem;
    }

    public void LoadPlaylist(
        PlaylistData data,
        bool startPlayback = true,
        bool saveSettings = true,
        bool checkMissingFiles = true,
        bool loadThumbnails = true)
    {
        _loading = true;
        try
        {
            _current = data;
            _settings.CurrentPlaylistId = data.Id;

            Playlist.Clear();

            // carrega os vídeos da playlist no drawer
            foreach (var i in data.Items)
            {
                Playlist.Add(new VideoItem
                {
                    Id = i.Id,
                    Title = i.Title,
                    Path = i.Path,
                    IsMissing = checkMissingFiles && !File.Exists(i.Path)
                });
            }

            var currentItem = data.CurrentItemId is { } currentItemId
                ? Playlist.FirstOrDefault(v => v.Id == currentItemId)
                : null;

            if (currentItem is null)
            {
                currentItem = Playlist.FirstOrDefault(v => !v.IsMissing) ?? Playlist.FirstOrDefault();
                data.PositionMs = 0;
            }

            CurrentFile = currentItem;
            data.CurrentItemId = currentItem?.Id;

            CurrentPlaylist = AddOrUpdatePlaylistItem(data);
            CurrentPlaylistThumbnail = data.Thumbnail;

        }
        finally
        {
            _loading = false;
            if (startPlayback)
                Play(CurrentFile);
        }

        if (IsPlaylistDrawerOn)
            _ = LoadPlaylistThumbnailAsync(data);

        if (loadThumbnails && IsVideoDrawerOn)
            _ = LoadThumbnailsAsync(Playlist.Where(v => !v.IsMissing).ToList(), data);
        if (saveSettings)
            FlushSave();
    }

    public void LoadPlaylist(
        PlaylistItem playlist,
        bool startPlayback = true,
        bool saveSettings = true)
    {
        var data = _settings.Playlists.FirstOrDefault(savedPlaylist => savedPlaylist.Id == playlist.Id);
        if (data is null)
        {
            data = new PlaylistData
            {
                Id = playlist.Id,
                Name = playlist.Title,
                Items = playlist.MediaPaths.Select(path => new ItemData
                {
                    Path = path,
                    Title = System.IO.Path.GetFileNameWithoutExtension(path)
                }).ToList(),
                Thumbnail = playlist.Thumbnail
            };

            if (data.Items.Count > 0)
                data.CurrentItemId = data.Items[0].Id;

            _settings.Playlists.Add(data);
        }

        LoadPlaylist(data, startPlayback, saveSettings);
    }

    public void CreatePlaylistFromCurrent(string name)
    {
        var currentItemIndex = CurrentFile is null ? -1 : Playlist.IndexOf(CurrentFile);
        var data = new PlaylistData
        {
            Name = name,
            Items = Playlist.Select(item => new ItemData
            {
                Path = item.Path,
                Title = item.Title
            }).ToList(),
            Thumbnail = CurrentPlaylistThumbnail
        };
        if (currentItemIndex >= 0 && currentItemIndex < data.Items.Count)
            data.CurrentItemId = data.Items[currentItemIndex].Id;

        _settings.Playlists.Add(data);
        _settings.CurrentPlaylistId = data.Id;
        _current = data;
        CurrentPlaylist = AddOrUpdatePlaylistItem(data);
        FlushSave();
    }

    public void DeletePlaylist(PlaylistItem playlist)
    {
        var playlistId = playlist.Id;
        var isCurrentPlaylist = _current?.Id == playlistId ||
                                _settings.CurrentPlaylistId == playlistId;

        Playlists.Remove(playlist);
        _settings.Playlists.RemoveAll(data => data.Id == playlistId);

        if (isCurrentPlaylist)
        {
            _current = null;
            _settings.CurrentPlaylistId = null;
            CurrentPlaylist = null;
            CurrentPlaylistThumbnail = null;
        }

        FlushSave();
    }

    public void LoadMediaFiles(IEnumerable<VideoItem> items)
    {
        _loading = true;
        try
        {
            _current = null;
            _settings.CurrentPlaylistId = null;
            CurrentPlaylist = null;
            CurrentPlaylistThumbnail = null;
            Playlist.Clear();

            foreach (var item in items)
                Playlist.Add(item);

            CurrentFile = Playlist.FirstOrDefault();
        }
        finally
        {
            _loading = false;
        }

        if (CurrentFile is not null)
            Play(CurrentFile);

        FlushSave();
    }

    private async Task LoadPlaylistThumbnailAsync(PlaylistData data)
    {
        if (data.Thumbnail is not null)
            return;

        var item = await Task.Run(() =>
            data.Items.FirstOrDefault(candidate => File.Exists(candidate.Path)));

        if (item is null)
            return;

        var thumbnail = await Task.Run(() => ThumbnailService.GetOrCreateAsync(item.Path));
        if (thumbnail is null)
            return;

        data.Thumbnail = thumbnail;
        var playlistItem = Playlists.FirstOrDefault(candidate => candidate.Id == data.Id);
        if (playlistItem is not null)
            playlistItem.Thumbnail = thumbnail;

        if (ReferenceEquals(_current, data))
            CurrentPlaylistThumbnail = thumbnail;
    }

    // pra carregar as thumbs dos vídeos quando uma playlist for carregada:
    private async Task LoadThumbnailsAsync(List<VideoItem> items, PlaylistData data)
    {
        foreach (var item in items)
            item.Thumbnail = await Task.Run(() => ThumbnailService.GetOrCreateAsync(item.Path));

        if (ReferenceEquals(_current, data))
        {
            data.Thumbnail = items.Select(item => item.Thumbnail).FirstOrDefault(thumbnail => thumbnail is not null);
            CurrentPlaylistThumbnail = data.Thumbnail;
            var playlistItem = Playlists.FirstOrDefault(item => item.Id == data.Id);
            if (playlistItem is not null)
                playlistItem.Thumbnail = data.Thumbnail;
        }
    }

    private async Task ValidateStartupPlaylistAsync(PlaylistData data)
    {
        var items = Playlist.ToArray();
        var missingItemIds = await Task.Run(() => items
            .Where(item => !File.Exists(item.Path))
            .Select(item => item.Id)
            .ToHashSet());

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!ReferenceEquals(_current, data))
                return;

            foreach (var item in items)
                item.IsMissing = missingItemIds.Contains(item.Id);

            if (CurrentFile is null || CurrentFile.IsMissing)
            {
                CurrentFile = Playlist.FirstOrDefault(item => !item.IsMissing);
                data.PositionMs = 0;
            }

            data.CurrentItemId = CurrentFile?.Id;
            if (CurrentFile is not null)
                Play(CurrentFile);

            if (IsVideoDrawerOn)
                _ = LoadThumbnailsAsync(Playlist.Where(item => !item.IsMissing).ToList(), data);
        });
    }

    private void SyncToSettings()
    {
        if (_current is null)
            return;

        _current.Items = Playlist
            .Select(v => new ItemData { Id = v.Id, Path = v.Path, Title = v.Title })
            .ToList();
        _current.CurrentItemId = CurrentFile?.Id;
        if (_current.CurrentItemId is null) _current.PositionMs = 0;
    }

    // debounce: vários eventos seguidos viram um único salvamento
    public async void RequestSave()
    {
        _saveCts?.Cancel();
        var cts = _saveCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(1500, cts.Token);
            FlushSave();
        }
        catch (OperationCanceledException) { }
    }

    public void FlushSave()
    {
        SyncToSettings();
        _settings.Preferences.Autoplay = AutoPlay;
        _settings.Preferences.Shuffle = Shuffle;
        _settings.Preferences.RepeatMode = AutoRepeat
            ? RepeatMode.One
            : Loop
                ? RepeatMode.All
                : RepeatMode.None;
        _settings.Preferences.Volume = _volume;
        _settings.Preferences.Muted = IsMuted;
        SettingsStore.Save(_settings);

        // ############################ TESTES ###########################
        Console.WriteLine("FlushSave debug");
        Console.WriteLine($"App Version: {_settings.Version}");
        Console.WriteLine($"Playlists saved:");
        foreach (var playlist in _settings.Playlists)
        {
            Console.WriteLine($"ID: {playlist.Id} | Name: {playlist.Name}");
        }
        Console.WriteLine($"Current Playlist ID: {_settings.CurrentPlaylistId}");

        // ################################################################
    }

    // chamado a cada ~5s a partir do timer do player, e ao trocar de mídia
    public void UpdateProgress(long positionMs)
    {
        if (_current is null)
            return;

        _current.CurrentItemId = CurrentFile?.Id;
        _current.PositionMs = CurrentFile is null ? 0 : Math.Max(0, positionMs);
        RequestSave();
    }

    public async Task<PlaylistData> ImportPlaylist(string name, IEnumerable<M3u8Playlist.Entry> entries)
    {
        var items = entries.Select(e => new ItemData
        {
            Path = e.Path,
            Title = e.Title ?? System.IO.Path.GetFileNameWithoutExtension(e.Path)
        }).ToList();
        var paths = items.Select(item => item.Path).ToList();

        var existingData = _settings.Playlists.FirstOrDefault(playlist =>
            HaveSameMediaPaths(playlist.Items.Select(item => item.Path), paths));
        if (existingData is not null)
        {
            LoadPlaylist(existingData);
            return existingData;
        }

        var existingDrawerItem = Playlists.FirstOrDefault(playlist =>
            HaveSameMediaPaths(playlist.MediaPaths, paths));

        if (existingDrawerItem is not null &&
            _settings.Playlists.FirstOrDefault(playlist => playlist.Id == existingDrawerItem.Id) is { } existingById)
        {
            LoadPlaylist(existingById);
            return existingById;
        }

        var data = new PlaylistData
        {
            Id = existingDrawerItem?.Id ?? Guid.NewGuid(),
            Name = name,
            Items = items,
            Thumbnail = existingDrawerItem?.Thumbnail,
        };

        data.CurrentItemId = data.Items.FirstOrDefault()?.Id;
        if (data.Items.FirstOrDefault() is { } firstItem)
            data.Thumbnail = await ThumbnailService.GetOrCreateAsync(firstItem.Path);

        _settings.Playlists.Add(data);
        LoadPlaylist(data);
        return data;
    }

    private static bool HaveSameMediaPaths(IEnumerable<string> first, IReadOnlyList<string> second) =>
        first.SequenceEqual(second, StringComparer.OrdinalIgnoreCase);

    public IEnumerable<ItemData> CurrentItems() =>
    Playlist.Select(v => new ItemData { Id = v.Id, Path = v.Path, Title = v.Title });

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

    private void OnVolumeChanged(object? sender, double volume)
    {
        if (!IsMuted)
        {
            if (SetField(ref _volume, Math.Clamp((int)volume, 0, 100), nameof(Volume)))
                OnPropertyChanged(nameof(VolumeControlValue));
        }
    }

    private void OnErrorOccurred(object? sender, string message) => LastError = message;

    // Este método verifica se o player está ou não no final da playlist, e executa comandos de acordo com estes casos.
    private void ReachedEndOfPlay()
    {
        if (CurrentFile is null || Playlist.Count == 0)
            return;

        int currentVideoIndex = Playlist.IndexOf(CurrentFile);
        int playlistLastIndex = Playlist.IndexOf(Playlist.Last());

        // Analisa se o índice para o próximo vídeo é menor ou igual ao índice do último vídeo da playlist
        // se sim, segue com a incrementação. Se não, o índice do próximo vídeo não atualiza.
        int nextVideoIndex = currentVideoIndex + 1 <= playlistLastIndex ? currentVideoIndex + 1 : currentVideoIndex;

        VideoItem nextVideo = Playlist[nextVideoIndex];

        Debug.WriteLine($"Current Media Index: {nextVideoIndex}\nPlaylist last Index: {playlistLastIndex}");
        // checagem básica de fim de playlist:
        if (currentVideoIndex == playlistLastIndex)
        {
            IsPlaylistEnd = true;
        }
        else
        {
            IsPlaylistEnd = false;
        }

        // TODO: ver possível refatoração desses blocos de if... else, para maior clareza.
        if (IsPlaylistEnd)
        {
            if (AutoRepeat)
            {
                Play(CurrentFile); // dá replay no vídeo final, caso autorepeat esteja ativado
            }
            else if (Loop)
            {
                CurrentFile = Playlist[0];
                Play(CurrentFile); // recomeça a playlist do zero
            }
            else if (Shuffle)
            {
                ShufflePlaylist(Playlist);
                CurrentFile = Playlist[0];
                Play(CurrentFile);
            }
            else
            {
                Play(CurrentFile);
                Stop();
            }
        }
        else
        {
            if (AutoPlay)
            {
                // Console.WriteLine($"Video pra tocar agora: {nextVideo.Title}\nVideo Index: {nextVideoIndex}");

                Play(nextVideo);

                // isto aqui é importante, do contrário "CurrentFile" será sempre o vídeo de índice 0.
                CurrentFile = nextVideo;
            }
            else if (AutoRepeat)
            {
                Play(CurrentFile);
            }
            else if (Shuffle)
            {
                ShufflePlaylist(Playlist);
                CurrentFile = Playlist[0];
                Play(CurrentFile);
            }
            else
            {
                Play(CurrentFile);
                Stop();
            }

        }

    }

    // NOTA: Algoritmo de permutação Fisher-Yates Shuffle (embaralhar), complexidade O(n), pra misturar os elementos da playlist. 
    // Este algoritmo é mais eficiente, neste caso, do que um simples Random (que normalmente possui algum viés, dependendo de como é implementado).
    public static void ShufflePlaylist<T>(ObservableCollection<T> list)
    {
        Random rnd = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rnd.Next(n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
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

public class MediaOptions
{
    public string autoPlay { get; set; }
    public string autoRepeat { get; set; }
    public string loop { get; set; }
    public string shuffle { get; set; }
}
