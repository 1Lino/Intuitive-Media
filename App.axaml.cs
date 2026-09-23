using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using IntuitiveMedia.ViewModels;
using IntuitiveMedia.Views;
using IntuitiveMedia.Infrastructure.VLC;
using Microsoft.Extensions.DependencyInjection;
using IntuitiveMedia.Core;

namespace IntuitiveMedia;

public partial class App : Application
{
    private VlcCoreInitializer? _vlcCore;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Precisa vir ANTES de resolver qualquer serviço/ViewModel que use IMediaPlayerService.
        _vlcCore = new VlcCoreInitializer();
        _vlcCore.EnsureInitialized(enableDebugLogs: false);

        var services = new ServiceCollection();
        services.AddSingleton(_vlcCore);
        services.AddTransient<IMediaPlayerService, VlcMediaPlayerService>();
        services.AddTransient<PlayerViewModel>();
        var provider = services.BuildServiceProvider();


        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            IMediaPlayerService playerService = new VlcMediaPlayerService(_vlcCore);

            var mainViewModel = new PlayerViewModel(playerService);
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };

            // quando a aplicação for finalizada, dá um dispose nos objetos MediaPlayer e LibVLC na memória pra evitar vazamento:
            desktop.ShutdownRequested += (sender, e) =>
            {
                _vlcCore.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}