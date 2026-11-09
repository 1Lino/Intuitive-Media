using Avalonia.Controls;
using IntuitiveMedia.ViewModels;

namespace IntuitiveMedia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();


        // NOTA: O redimensionamento dos controles ao maximizar a tela deve ocorrer aqui, pois somente PropertyChanged tem acesso ao evento de redimensionamento da tela, e esta tela é a MainWindow. Já quanto ao doubleTap ocorre em PlayerView mesmo, uma vez que deve ser tratado a questão do overlay dos controles do avalonia sobre o player do VLC.
        PropertyChanged += (s, e) =>
        {
            if (DataContext is PlayerViewModel vm)
            {
                if (GetTopLevel(this) is not Window window) return;

                if (e.Property == WindowStateProperty && WindowState == WindowState.Maximized)
                {
                    vm.WindowStateBeforeFullScreen = window.WindowState;

                    vm.PlaylistDrawerHeight = 400;
                    vm.PLaylistScrollHeight = 320;
                }
                else if (e.Property == WindowStateProperty && WindowState == WindowState.Normal)
                {
                    vm.WindowStateBeforeFullScreen = window.WindowState;

                    vm.PlaylistDrawerHeight = 200;
                    vm.PLaylistScrollHeight = 120;
                }
            }
        };
    }

    public void SetStartupStatus(string? message)
    {
        StartupStatus.Text = message;
        StartupStatus.IsVisible = message is not null;
    }

    public void SetPlayerViewModel(PlayerViewModel viewModel)
    {
        DataContext = viewModel;
        PlayerViewControl.SetPlayerViewModel(viewModel);
    }
}