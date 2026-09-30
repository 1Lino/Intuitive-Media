using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using IntuitiveMedia.Models;
using IntuitiveMedia.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

namespace IntuitiveMedia.Views;

public partial class MediaDrawer : UserControl
{
    private static readonly DataFormat<string> DragFormat =
        DataFormat.CreateStringApplicationFormat("intuitivemedia-video-item");

    private const double DragThreshold = 8;
    private Control? _pressedControl;

    private VideoItem? _pressedItem;
    private VideoItem? _draggedItem;
    private PointerPressedEventArgs? _pressedArgs;   // evento que será usado como gatilho do drag
    private Point _dragStart;

    public MediaDrawer() => InitializeComponent();

    private PlayerViewModel? Vm => DataContext as PlayerViewModel;

    private void Item_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: VideoItem item } c &&
            e.GetCurrentPoint(c).Properties.IsLeftButtonPressed)
        {
            _pressedItem = item;
            _pressedControl = c;
            _pressedArgs = e;
            _dragStart = e.GetPosition(this);
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

        // using var transfer = new DataTransfer();
        // transfer.Add(DataTransferItem.Create(DragFormat, _draggedItem.Title));

        // await DragDrop.DoDragDropAsync(trigger, transfer, DragDropEffects.Move);

        // _draggedItem = null;
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
        _pressedItem = null;
        _pressedArgs = null;
        _pressedControl = null;
    }

    private void Item_DragOver(object? sender, DragEventArgs e)
    {
        var valid = e.DataTransfer.Contains(DragFormat);
        e.DragEffects = valid ? DragDropEffects.Move : DragDropEffects.None;

        // destaca o alvo, exceto o próprio item arrastado
        if (sender is Control { DataContext: VideoItem target } c)
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
            sender is not Control { DataContext: VideoItem target } ||
            ReferenceEquals(source, target))
            return;

        var oldIndex = Vm.Playlist.IndexOf(source);
        var newIndex = Vm.Playlist.IndexOf(target);
        if (oldIndex >= 0 && newIndex >= 0)
            // Vm.Playlist.Move(oldIndex, newIndex);
            MoveWithAnimation(oldIndex, newIndex);
    }

    private void MoveWithAnimation(int oldIndex, int newIndex)
    {
        var playlist = Vm!.Playlist;

        // 1) Posição X de cada item ANTES do move
        var before = new Dictionary<VideoItem, double>();
        foreach (var item in playlist)
        {
            if (VideoList.ContainerFromItem(item) is Control c &&
                c.TranslatePoint(new Point(0, 0), VideoList) is { } p)
                before[item] = p.X;
        }

        // 2) Move e força o layout
        playlist.Move(oldIndex, newIndex);
        VideoList.UpdateLayout();

        // 3) Para cada item que mudou de posição, anima de "onde estava" até "onde está"
        foreach (var item in playlist)
        {
            if (!before.TryGetValue(item, out var oldX) ||
                VideoList.ContainerFromItem(item) is not Control container ||
                container.TranslatePoint(new Point(0, 0), VideoList) is not { } p)
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