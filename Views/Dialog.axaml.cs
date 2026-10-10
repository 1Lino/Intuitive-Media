using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IntuitiveMedia.Views;

public partial class Dialog : Window
{
    public string Nome => NomeTextBox.Text?.Trim() ?? "";

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
        if (NomeTextBox.IsVisible && string.IsNullOrWhiteSpace(Nome))
        {
            NomeTextBox.Focus();
            return;
        }

        Close(true);
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}