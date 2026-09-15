using System;
using Avalonia.Controls;
using Avalonia.Input;
using IntuitiveMedia.ViewModels;

namespace IntuitiveMedia.Views;

public partial class PlayerMediaMenu : UserControl
{

    public PlayerMediaMenu()
    {
        InitializeComponent();
    }

    private void OnMouseOver(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsPointerOverControls = true;
            Console.WriteLine($"Is mouse over control: {vm.IsPointerOverControls}");
        }
    }

    private void OnMouseExit(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsPointerOverControls = false;
            Console.WriteLine($"Is mouse over control: {vm.IsPointerOverControls}");
        }
    }
}