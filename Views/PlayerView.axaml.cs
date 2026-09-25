using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using IntuitiveMedia.ViewModels;
using LibVLCSharp.Shared;

namespace IntuitiveMedia.Views;

public partial class PlayerView : UserControl
{
    private readonly DispatcherTimer _hideTimer;
    public PlayerView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;

        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };

        _hideTimer.Tick += HideTimer_Tick;

    }

    private void PointerMovedIntoOverlay(object? sender, PointerEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            vm.AreControlsVisible = true;

            // poderia ser sender.Cursor, o que faria referência ao objeto que disparou o evento, aqui, no caso, ControlsOverlay.
            ControlsOverlay.Cursor = new Cursor(StandardCursorType.Arrow); // restaura cursor ao mover ponteiro.

            // Console.WriteLine("Pointer moved over the window!");
            // Console.WriteLine($"Should controls turn visible? {vm.AreControlsVisible}");
            // Console.WriteLine($"Is pointer over a control? {vm.IsPointerOverControls}");

        }
        // Console.WriteLine("DataContext is not PlayerViewModel");
        // Console.WriteLine($"[PlayerView] DataContext mudou para: {DataContext?.GetType().FullName ?? "null"}");

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
        if (DataContext is PlayerViewModel vm)
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

        if (DataContext is PlayerViewModel vm)
        {
            if (!vm.IsPointerOverControls)
            {
                vm.AreControlsVisible = false;

                ControlsOverlay.Cursor = new Cursor(StandardCursorType.None); // faz o cursor sumir
                // Console.WriteLine("Controls are now invisible");
                return;
            }
            // Console.WriteLine("Controls remain visible");
        }
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            // Cast isolado aqui se o handle nativo mudar de tipo algum dia,
            // só este arquivo precisa mudar.
            VideoViewControl.MediaPlayer = vm.NativePlayerHandle as MediaPlayer;
        }
    }

    //END
}