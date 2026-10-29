using Avalonia.Controls;
using IntuitiveMedia.Infrastructure.VLC;
using IntuitiveMedia.ViewModels;

namespace IntuitiveMedia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (DataContext is PlayerViewModel vm)
        {
            vm.Initialize();
            Closing += (_, _) => (DataContext as PlayerViewModel)?.FlushSave();
        }
    }
}