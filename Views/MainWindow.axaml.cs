using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using IntuitiveMedia.ViewModels;

namespace IntuitiveMedia.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _hideTimer;
    private bool _isPointerOverControls;
    public MainWindow()
    {
        InitializeComponent();

        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };

        _hideTimer.Tick += HideTimer_Tick;
    }

    private void PointerMovedIntoWindow(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.AreControlsVisible = true;
        }

        // Mouse activity resets the inactivity timer.
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void HideTimer_Tick(object? sender, EventArgs e)
    {
        _hideTimer.Stop();

        if (!_isPointerOverControls)
            if (DataContext is MainViewModel vm)
            {
                vm.AreControlsVisible = false;
            }
    }


}