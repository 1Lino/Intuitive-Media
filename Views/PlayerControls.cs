using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
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

    private void MutarMidia(object? sender, RoutedEventArgs e)
    {

        if (DataContext is PlayerViewModel vm)
        {
            vm.IsMuted = !vm.IsMuted;
        }

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

            foreach (var file in files)
            {
                // var item = new VideoItem { Title = file.Name, Path = file.Path.ToString() };
                var item = new VideoItem { Title = file.Name, Path = file.Path.LocalPath };
                novos.Add(item);
            }

            vm.LoadMediaFiles(novos);

            // miniaturas: uma por vez, aparecem conforme ficam prontas
            foreach (var item in novos)
            {
                item.Thumbnail = await ThumbnailService.GetOrCreateAsync(item.Path);
            }
            // pega a primeira miniatura e define como thumb para a playlist atual.
            vm.CurrentPlaylistThumbnail = await ThumbnailService.GetOrCreateAsync(novos[0].Path);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);   // exceção em async void sem try/catch derruba o app
        }
    }

    private void OnMediaOptionsOpened(object? sender, EventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            vm.IsMediaOptionsOn = true;
        }
    }

    private void OnMediaOptionsClosed(object? sender, CancelEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            vm.IsMediaOptionsOn = false;
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

    private void ProximaMidia(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PlayerViewModel vm || vm.Playlist.Count == 0)
            return;

        var currentVideoIndex = vm.CurrentFile is null ? 0 : vm.Playlist.IndexOf(vm.CurrentFile);
        if (currentVideoIndex < 0)
            currentVideoIndex = 0;

        var nextVideoIndex = Math.Min(currentVideoIndex + 1, vm.Playlist.Count - 1);
        VideoItem nextVideo = vm.Playlist[nextVideoIndex];
        vm.Play(nextVideo);
        vm.CurrentFile = nextVideo;
    }

    private void MidiaAnterior(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PlayerViewModel vm || vm.Playlist.Count == 0)
            return;

        var currentVideoIndex = vm.CurrentFile is null ? 0 : vm.Playlist.IndexOf(vm.CurrentFile);
        if (currentVideoIndex < 0)
            currentVideoIndex = 0;

        var previousVideoIndex = Math.Max(currentVideoIndex - 1, 0);
        VideoItem previousVideo = vm.Playlist[previousVideoIndex];
        vm.Play(previousVideo);
        vm.CurrentFile = previousVideo;
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