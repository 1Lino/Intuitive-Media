using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using IntuitiveMedia.ViewModels;

namespace IntuitiveMedia.Views;

public partial class PlaylistMenu : UserControl
{

    public PlaylistMenu()
    {
        InitializeComponent();
    }

    private void TogglePlaylistDrawerVisibility(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            vm.IsPlaylistDrawerOn = !vm.IsPlaylistDrawerOn;
        }

    }

    private void OnMouseOver(object? sender, PointerEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            vm.IsPointerOverControls = true;
            // Console.WriteLine($"Is mouse over control: {vm.IsPointerOverControls}");
        }
    }

    private void OnMouseExit(object? sender, PointerEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            vm.IsPointerOverControls = false;
            // Console.WriteLine($"Is mouse over control: {vm.IsPointerOverControls}");
        }
    }
}