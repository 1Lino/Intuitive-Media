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
using System.Threading.Tasks;

namespace IntuitiveMedia;

public partial class App : Application
{
    private VlcCoreInitializer? _vlcCore;
    private PlayerViewModel? _mainViewModel;
    private Task? _startupTask;
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
            var mainWindow = new MainWindow
            {
                DataContext = null,
            };
            desktop.MainWindow = mainWindow;

            mainWindow.Opened += (_, _) =>
            {
                _startupTask = InitializePlayerAsync(mainWindow);
            };
            mainWindow.Closing += (_, _) => _mainViewModel?.FlushSave();

            desktop.ShutdownRequested += (_, _) =>
            {
                _shutdownRequested = true;
                if (_startupTask?.IsCompleted != false)
                    DisposeResources();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializePlayerAsync(MainWindow mainWindow)
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
            mainViewModel.Initialize(settings, deferStartupWork: true);
            mainViewModel.AreControlsVisible = true;
            mainWindow.SetPlayerViewModel(mainViewModel);
            mainWindow.SetStartupStatus(null);
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

    private void DisposeResources()
    {
        if (_resourcesDisposed)
            return;

        _resourcesDisposed = true;
        _mainViewModel?.Dispose();
        _vlcCore?.Dispose();
    }
}