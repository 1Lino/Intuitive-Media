using System;
using Avalonia.Controls;
using Avalonia.Input;
using IntuitiveMedia.ViewModels;

namespace IntuitiveMedia.Views;

// Todo axaml separado precisa de uma classe dessas.
public partial class PlayerControls : UserControl
{
    public PlayerControls()
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