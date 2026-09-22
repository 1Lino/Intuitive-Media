using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IntuitiveMedia.ViewModels;
using LibVLCSharp.Shared;

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
        if (DataContext is MediaDrawerModel vm)
        {
            var topLevel = TopLevel.GetTopLevel(this); // TopLevel é o window. StorageProvider só existe no window.

            if (topLevel == null)
                return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Selecionar arquivo",
                    AllowMultiple = false
                });

            if (files.Count > 0)
            {
                // acessa a lista e manda somente o path dos arquivos selecionados pra ela.
                vm._files.AddRange(files.Select(file => file.Path.LocalPath));

                foreach (var file in vm._files)
                {
                    Console.WriteLine(file);
                }

            }

            // TODO: estas três linhas abaixo podem se tornar um método separado, sendo que vm._files... etc deve ser um argumento passado pra esse método, de modo que, para carregar mídia, usamos vm._files.Last() pra carregar a última mídia separada, e no caso de outros controles carregamos outros arquivos da lista (por exemplo, no caso do controle de "próximo", ele deve saber qual é o item atual da lista sendo executado e então executar o item do próximo índice da lista, e assim por diante).
            using var media = new Media(vm.LibVLC, new Uri(vm._files.Last()));
            if (media == null) return;
            vm.MediaPlayer.Play(media);
            vm.isMediaPaused = false;

            Console.WriteLine("Clicked the load button to load a video.");
        }
    }

    private void PausarMidia(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MediaDrawerModel vm)
        {
            if (vm.MediaPlayer.CanPause)
                vm.isMediaPaused = !vm.isMediaPaused;

            if (vm.isMediaPaused)
                vm.MediaPlayer.Pause();
            else
                vm.MediaPlayer.Play();
        }
    }

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