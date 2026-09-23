using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IntuitiveMedia.ViewModels;

namespace IntuitiveMedia.Views;

// Todo axaml separado precisa de uma classe dessas.
public partial class PlayerControls : UserControl
{
    public PlayerControls()
    {
        InitializeComponent();
    }

    private async void CarregarMidia(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            var topLevel = TopLevel.GetTopLevel(this); // TopLevel é o window. StorageProvider só existe no window.

            if (topLevel == null)
                return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Selecionar arquivo",
                    AllowMultiple = false // por enquanto, só será permitido 1 arquivo, para fins de teste, mas será possível selecionar vários para uma playlist, futuramente.
                });

            if (files.Count > 0)
            {
                // acessa a lista e manda somente o path dos arquivos selecionados pra ela.
                vm._playlist.AddRange(files.Select(file => file.Path.LocalPath));

                foreach (var file in vm._playlist)
                {
                    vm.currentMedia = vm._playlist.Last();
                    // Console.WriteLine($"You're gonna watch: {file}");
                    // Console.WriteLine($"State of the MediaPlayer: {vm.MediaPlayer.State}");
                }
            }
            else
            {
                return; // pois não se deve seguir adiante caso haja 0 arquivos selecionados, do contrário incorreria em um exception de operação inválida, já que a playlist estaria vazia e o método abaixo tentaria reproduzir uma mídia que não existe.
            }

            vm.Play(new Uri(vm.currentMedia));

            // Console.WriteLine("Clicked the load button to load a video.");
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