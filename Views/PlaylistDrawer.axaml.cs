using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using IntuitiveMedia.Models;
using IntuitiveMedia.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Animation;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using System.Collections.Generic;

namespace IntuitiveMedia.Views;

public partial class PlaylistDrawer : UserControl
{
    private static readonly DataFormat<string> DragFormat =
        DataFormat.CreateStringApplicationFormat("intuitivemedia-playlist-item");

    private const double DragThreshold = 8;
    private Control? _pressedControl;

    private PlaylistItem? _pressedItem;
    private PlaylistItem? _draggedItem;
    private PointerPressedEventArgs? _pressedArgs;   // evento que será usado como gatilho do drag
    private Point _dragStart;

    public PlaylistDrawer() => InitializeComponent();

    private PlayerViewModel? Vm => DataContext as PlayerViewModel;

    private void Item_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: PlaylistItem item } c &&
            e.GetCurrentPoint(c).Properties.IsLeftButtonPressed)
        {
            _pressedItem = item;
            _pressedControl = c;
            _pressedArgs = e;
            _dragStart = e.GetPosition(this);

            Vm.IsMouseDragging = true;
            Debug.WriteLine($"Mouse is dragging playlist: {Vm.IsMouseDragging}");
        }
    }

    private async void Item_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedItem is null || _pressedArgs is null ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var delta = e.GetPosition(this) - _dragStart;
        if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold)
            return;

        _draggedItem = _pressedItem;
        var trigger = _pressedArgs; // trigger => PointerPressedEventArgs
        var draggedControl = _pressedControl;
        _pressedItem = null;
        _pressedArgs = null;
        _pressedControl = null;

        if (draggedControl is not null)
            SetClass(draggedControl, "dragging", true);

        try
        {
            using var transfer = new DataTransfer();
            transfer.Add(DataTransferItem.Create(DragFormat, _draggedItem.Title));

            await DragDrop.DoDragDropAsync(trigger, transfer, DragDropEffects.Move);
        }
        finally
        {
            if (draggedControl is not null)
                SetClass(draggedControl, "dragging", false);
            _draggedItem = null;
        }

    }

    private static void SetClass(Control control, string name, bool enabled)
    {
        if (enabled)
        {
            if (!control.Classes.Contains(name))
                control.Classes.Add(name);
        }
        else
        {
            control.Classes.Remove(name);
        }
    }

    private void Item_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var playlistToPlay = _pressedItem;
        var isClick = playlistToPlay is not null &&
                      sender is Control control &&
                      ReferenceEquals(control, _pressedControl) &&
                      ReferenceEquals(control.DataContext, playlistToPlay) &&
                      e.InitialPressMouseButton == MouseButton.Left &&
                      _draggedItem is null;

        _pressedItem = null;
        _pressedArgs = null;
        _pressedControl = null;

        if (isClick && playlistToPlay is not null && Vm is { } vm)
        {
            vm.CurrentPlaylist = playlistToPlay;
            // uma vez clicado numa playlist, começa a executar o primeiro vídeo dela, mas, antes disso, deve ser carregada propriamente. Este Play abaixo é só um teste.
            // vm.Play(playlistToPlay[0]); 
            vm.IsMouseDragging = false;
        }
    }

    private void Item_DragOver(object? sender, DragEventArgs e)
    {
        var valid = e.DataTransfer.Contains(DragFormat);
        e.DragEffects = valid ? DragDropEffects.Move : DragDropEffects.None;

        // destaca o alvo, exceto o próprio item arrastado
        if (sender is Control { DataContext: PlaylistItem target } c)
            SetClass(c, "dragover", valid && !ReferenceEquals(target, _draggedItem));
    }

    private void Item_DragLeave(object? sender, DragEventArgs e)
    {
        if (sender is Control c)
            SetClass(c, "dragover", false);
    }

    private void Item_Drop(object? sender, DragEventArgs e)
    {
        if (sender is Control c)
            SetClass(c, "dragover", false);

        if (Vm is null ||
            _draggedItem is not { } source ||
            sender is not Control { DataContext: PlaylistItem target } ||
            ReferenceEquals(source, target))
            return;

        var oldIndex = Vm.Playlists.IndexOf(source);
        var newIndex = Vm.Playlists.IndexOf(target);
        if (oldIndex >= 0 && newIndex >= 0)
            MoveWithAnimation(oldIndex, newIndex);

        Vm.IsMouseDragging = false;
        Debug.WriteLine($"Mouse is dragging media: {Vm.IsMouseDragging}");
    }

    private void MoveWithAnimation(int oldIndex, int newIndex)
    {
        var playlist = Vm!.Playlists;

        // 1) Posição X de cada item ANTES do move
        var before = new Dictionary<PlaylistItem, double>();
        foreach (var item in playlist)
        {
            if (PlaylistList.ContainerFromItem(item) is Control c &&
                c.TranslatePoint(new Point(0, 0), PlaylistList) is { } p)
                before[item] = p.X;
        }

        // 2) Move e força o layout
        playlist.Move(oldIndex, newIndex);
        PlaylistList.UpdateLayout();

        // 3) Para cada item que mudou de posição, anima de "onde estava" até "onde está"
        foreach (var item in playlist)
        {
            if (!before.TryGetValue(item, out var oldX) ||
                PlaylistList.ContainerFromItem(item) is not Control container ||
                container.TranslatePoint(new Point(0, 0), PlaylistList) is not { } p)
                continue;

            // diferença entre as posições x dos objetos a serem transladados
            var delta = oldX - p.X;
            if (Math.Abs(delta) < 0.5)
                continue;

            var transform = new TranslateTransform(delta, 0);
            container.RenderTransform = transform;

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(200),
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter(TranslateTransform.XProperty, delta) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters = { new Setter(TranslateTransform.XProperty, 0.0) }
                    }
                }
            };

            _ = RunAndReset(animation, container);
        }
    }
    private static async Task RunAndReset(Animation animation, Control target)
    {
        await animation.RunAsync(target);
        target.RenderTransform = null;   // pra limpar o deslocamento inicial quando terminar a animação
    }
}