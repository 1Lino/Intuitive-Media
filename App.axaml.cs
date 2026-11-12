using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using IntuitiveMedia.Core;
using IntuitiveMedia.Infrastructure.VLC;
using IntuitiveMedia.Models;
using IntuitiveMedia.Services;
using IntuitiveMedia.ViewModels;
using IntuitiveMedia.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IntuitiveMedia;

public partial class App : Application
{
    private VlcCoreInitializer? _vlcCore;
    private PlayerViewModel? _mainViewModel;
    private Task? _startupTask;
    private readonly CancellationTokenSource _startupCancellation = new();
    private bool _shutdownRequested;
    private bool _resourcesDisposed;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _vlcCore = new VlcCoreInitializer();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var startupArgs = desktop.Args ?? Array.Empty<string>();
            var mainWindow = new MainWindow
            {
                DataContext = null,
            };
            desktop.MainWindow = mainWindow;

            mainWindow.Opened += (_, _) =>
            {
                _startupTask = InitializePlayerAsync(mainWindow, startupArgs);
            };
            mainWindow.Closing += (_, _) => _mainViewModel?.FlushSave();

            desktop.ShutdownRequested += (_, _) =>
            {
                _shutdownRequested = true;
                _startupCancellation.Cancel();
                if (_startupTask?.IsCompleted != false)
                    DisposeResources();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializePlayerAsync(MainWindow mainWindow, string[] startupArgs)
    {
        try
        {
            var vlcCore = _vlcCore
                ?? throw new InvalidOperationException("O inicializador do VLC não foi criado.");
            var settingsTask = Task.Run(SettingsStore.Load);
            var vlcInitializationTask = Task.Run(() => vlcCore.EnsureInitialized(enableDebugLogs: false));
            await Task.WhenAll(settingsTask, vlcInitializationTask);

            if (_shutdownRequested)
                return;

            AppSettings settings = await settingsTask;

            var mainViewModel = new PlayerViewModel(new VlcMediaPlayerService(vlcCore));
            _mainViewModel = mainViewModel;
            mainViewModel.Initialize(settings);
            mainViewModel.AreControlsVisible = true;
            mainWindow.SetPlayerViewModel(mainViewModel);
            mainWindow.SetStartupStatus(null);
            await mainViewModel.LoadPlaylistsAsync(_startupCancellation.Token);
            OpenStartupMediaFiles(mainViewModel, startupArgs);
        }
        catch (OperationCanceledException) when (_shutdownRequested)
        {
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Falha ao inicializar o player: {ex}");
            if (!_shutdownRequested)
                mainWindow.SetStartupStatus($"Falha ao inicializar o player: {ex.Message}");
        }
        finally
        {
            if (_shutdownRequested)
                DisposeResources();
        }
    }

    private static void OpenStartupMediaFiles(PlayerViewModel viewModel, string[] startupArgs)
    {
        var paths = startupArgs
            .Where(argument => !string.IsNullOrWhiteSpace(argument))
            .Select(TryGetExistingFilePath)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (paths.Length == 0)
            return;

        viewModel.LoadMediaFiles(paths
            .Select(path => new VideoItem
            {
                Title = Path.GetFileName(path),
                Path = path,
            })
            .ToArray());
    }

    private static string? TryGetExistingFilePath(string argument)
    {
        try
        {
            var path = Path.GetFullPath(argument);
            if (File.Exists(path))
                return path;

            Trace.TraceWarning($"O argumento de inicialização não é um arquivo existente: {argument}");
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
        {
            Trace.TraceWarning($"Não foi possível interpretar o arquivo recebido na inicialização '{argument}': {ex.Message}");
        }

        return null;
    }

    private void DisposeResources()
    {
        if (_resourcesDisposed)
            return;

        _resourcesDisposed = true;
        _mainViewModel?.Dispose();
        _vlcCore?.Dispose();
        _startupCancellation.Dispose();
    }
}