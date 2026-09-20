using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using IntuitiveMedia.ViewModels;
using IntuitiveMedia.Views;

namespace IntuitiveMedia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = new MediaDrawerModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };

            // quando a aplicação for finalizada, dá um dispose nos objetos MediaPlayer e LibVLC na memória pra evitar vazamento:
            desktop.ShutdownRequested += (sender, e) =>
            {
                mainViewModel.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}