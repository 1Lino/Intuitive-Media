using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IntuitiveMedia.Models;
using IntuitiveMedia.Services;
using IntuitiveMedia.ViewModels;

namespace IntuitiveMedia.Views;

// Todo UserControl precisa de uma classe
public partial class PlayerControls : UserControl
{
    public PlayerControls()
    {
        InitializeComponent();
    }

    private async void CarregarMidia(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is not PlayerViewModel vm)
                return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Selecionar arquivo",
                    AllowMultiple = true
                });

            if (files.Count == 0)
                return;

            var novos = new List<VideoItem>(); // lista que será iterada para gerar as thumbnails

            vm.Playlist.Clear(); // zera a playlista toda vez que o usuário abrir o modal de seleção de vídeo.
            foreach (var file in files)
            {
                var item = new VideoItem { Title = file.Name, Path = file.Path.ToString() };
                vm.Playlist.Add(item);
                novos.Add(item);
            }

            vm.CurrentFile = vm.Playlist.Last();
            vm.Play(vm.CurrentFile);

            // miniaturas: uma por vez, aparecem conforme ficam prontas
            foreach (var item in novos)
            {
                item.PropertyChanged += (_, e) => Console.WriteLine($"[VideoItem] mudou: {e.PropertyName}");
                item.Thumbnail = await ThumbnailService.GenerateAsync(item.Path);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);   // exceção em async void sem try/catch derruba o app
        }
    }

    private void PausarMidia(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            if (vm.State == Core.PlaybackState.Playing)
                vm.Pause();
            else
                vm.Resume();

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