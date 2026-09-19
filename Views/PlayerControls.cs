using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using IntuitiveMedia.ViewModels;
using LibVLCSharp.Shared;

namespace IntuitiveMedia.Views;

// Todo axaml separado precisa de uma classe dessas.
public partial class PlayerControls : UserControl
{
    // private readonly LibVLC _libVLC;
    // private readonly MediaPlayer _mediaPlayer;

    public PlayerControls()
    {
        // _libVLC = new LibVLC();
        // _mediaPlayer = new MediaPlayer(_libVLC);
        InitializeComponent();
    }

    private void LoadMedia(object? sender, RoutedEventArgs e)
    {
        // TODO: deve haver uma propriedade em MediaDrawerModel que guarda o endereço do vídeo a ser carregado pelo viewer.
        // aqui é onde devemos chamar um LoadFile para puxar tal endereço e então mandar pra propriedade. A propriedade pode ser uma lista, de modo que o viewer possa acessar a lista depois, mas, por padrão, o primeiro índice da lista é que deve tocar.
        if (DataContext is MediaDrawerModel vm)
        {
            Console.WriteLine("Clicked the load button to load a video.");
        }
    }

    // private void PlayMedia(object? sender, VisualTreeAttachmentEventArgs e)
    // {
    //     if (DataContext is MediaDrawerModel vm)
    //     {
    //         var media = new Media(_libVLC, new Uri("https://www.w3schools.com/html/mov_bbb.mp4"));
    //         _mediaPlayer.Play(media);
    //         Console.WriteLine("Clicked the Play button!");
    //     }
    // }

    private void OnMouseOver(object? sender, PointerEventArgs e)
    {
        if (DataContext is MediaDrawerModel vm)
        {
            vm.IsPointerOverControls = true;
            Console.WriteLine($"Is mouse over control: {vm.IsPointerOverControls}");
        }
    }

    private void OnMouseExit(object? sender, PointerEventArgs e)
    {
        if (DataContext is MediaDrawerModel vm)
        {
            vm.IsPointerOverControls = false;
            Console.WriteLine($"Is mouse over control: {vm.IsPointerOverControls}");
        }
    }
}