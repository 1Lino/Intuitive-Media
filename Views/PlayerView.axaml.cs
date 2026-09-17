using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using IntuitiveMedia.ViewModels;

namespace IntuitiveMedia.Views;

public partial class PlayerView : UserControl
{
    private readonly DispatcherTimer _hideTimer;
    public PlayerView()
    {
        InitializeComponent();

        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };

        _hideTimer.Tick += HideTimer_Tick;

        AttachedToVisualTree += (_, _) =>
        {
            Debug.WriteLine(DataContext?.GetType().FullName);
        };
    }

    private void PointerMovedIntoWindow(object? sender, PointerEventArgs e)
    {
        if (DataContext is MediaDrawerModel vm)
        {
            vm.AreControlsVisible = true;
            Console.WriteLine("Pointer moved into window!");
        }

        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void HideTimer_Tick(object? sender, EventArgs e)
    {
        _hideTimer.Stop();

        if (DataContext is MediaDrawerModel vm)
        {
            if (!vm.IsPointerOverControls)
            {
                vm.AreControlsVisible = false;
                Console.WriteLine("Controls are now invisible");
                return;
            }
            Console.WriteLine("Controls remain visible");
        }
        //END
    }
}