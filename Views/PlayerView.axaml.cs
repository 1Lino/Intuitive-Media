using System;
using System.Diagnostics;
using System.Threading.Tasks;
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

        // AttachedToVisualTree += (_, _) =>
        // {
        //     Debug.WriteLine(DataContext?.GetType().FullName);
        // };
    }

    private void PointerMovedIntoWindow(object? sender, PointerEventArgs e)
    {
        if (DataContext is MediaDrawerModel vm)
        {
            vm.AreControlsVisible = true;
            // Console.WriteLine("Pointer moved over the window!");
            // Console.WriteLine($"Is pointer over a control? {vm.IsPointerOverControls}");
        }

        _hideTimer.Stop();
        _hideTimer.Start();
    }

    // O VideoView do LibVLC usa uma superfície nativa de vídeo, enquanto este
    // Grid transparente funciona como overlay Avalonia para capturar o mouse.
    // Nessa combinação, um único double-click pode chegar como dois eventos
    // DoubleTapped em poucos milissegundos. O debounce abaixo descarta apenas
    // essa segunda entrega duplicada, evitando alternar o fullscreen duas vezes.
    private readonly Stopwatch _doubleTapTimer = Stopwatch.StartNew();
    private TimeSpan _lastDoubleTap;
    private TimeSpan DoubleTapDebounce = TimeSpan.FromMilliseconds(100); // define um timespan de 100 milisegundos como debounce
    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Debounce para o evento duplicado observado no overlay do VideoView.
        var now = _doubleTapTimer.Elapsed;
        if (now - _lastDoubleTap < DoubleTapDebounce)
            return;

        _lastDoubleTap = now; // importante atualizar esse parâmetro

        // finalmente o bloco que leva ao comando de fullscreen:
        if (DataContext is MediaDrawerModel vm)
        {
            if (vm.IsPointerOverControls) return; // Pra impedir fullscreen se usuário clicar duas vezes em área de controles.

            if (TopLevel.GetTopLevel(this) is not Window window) // puxa a window pelo TopLevel desse objeto e disponibiliza como variável local.
                return;

            // toggle básico:
            window.WindowState = window.WindowState == WindowState.Normal ?
            WindowState.FullScreen : WindowState.Normal;
        }
    }

    private void HideTimer_Tick(object? sender, EventArgs e)
    {
        _hideTimer.Stop();

        if (DataContext is MediaDrawerModel vm)
        {
            if (!vm.IsPointerOverControls)
            {
                vm.AreControlsVisible = false;
                // Console.WriteLine("Controls are now invisible");
                return;
            }
            // Console.WriteLine("Controls remain visible");
        }
        //END
    }
}