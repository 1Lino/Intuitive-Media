using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using IntuitiveMedia.ViewModels;

namespace IntuitiveMedia.Views;

public partial class Dialog : Window
{
    public string Nome => NomeTextBox.Text?.Trim() ?? "";
    public static string TextBoxValue { get; private set; }

    private PlayerViewModel? Vm => DataContext as PlayerViewModel;

    public Dialog(string mensagem, bool modoAdicionar, string? nomeInicial = null)
    {
        InitializeComponent();

        MensagemText.Text = mensagem;
        NomeTextBox.IsVisible = modoAdicionar;

        ConfirmarButton.Content = modoAdicionar ? "OK" : "Sim";

        CancelarButton.Content = modoAdicionar ? "Cancelar" : "Não";

        if (modoAdicionar)
        {
            NomeTextBox.Text = nomeInicial ?? "";
        }
    }

    private void Confirmar_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm is null)
        {
            Debug.WriteLine("O contexto do player não está disponível.");
            return;
        }

        if (Vm.Playlist.Count == 0)
        {
            // TODO: messageBox dizendo algo como "Não é possível criar playlist vazia";
            Debug.WriteLine("Não é possível criar playlist vazia");
            return;
        }

        if (NomeTextBox.IsVisible && string.IsNullOrWhiteSpace(Nome))
        {
            NomeTextBox.Focus();
            Debug.WriteLine("Nome inválido!");
            return;
        }
        TextBoxValue = Nome;

        Close(true);
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}