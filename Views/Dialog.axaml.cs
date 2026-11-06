using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using IntuitiveMedia.ViewModels;

namespace IntuitiveMedia.Views;

public partial class Dialog : Window
{
    public string Nome => NomeTextBox.Text?.Trim() ?? "";
    public static string TextBoxValue { get; private set; }

    private readonly bool _modoAdicionar;
    private PlayerViewModel? Vm => DataContext as PlayerViewModel;

    public Dialog(string mensagem, bool modoAdicionar, string? nomeInicial = null)
    {
        InitializeComponent();

        _modoAdicionar = modoAdicionar;
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
        if (_modoAdicionar && Vm is null)
        {
            Debug.WriteLine("O contexto do player não está disponível.");
            return;
        }

        if (_modoAdicionar && Vm!.Playlist.Count == 0)
        {
            Debug.WriteLine("Não é possível criar playlist vazia");
            return;
        }

        if (_modoAdicionar && string.IsNullOrWhiteSpace(Nome))
        {
            NomeTextBox.Focus();
            Debug.WriteLine("Nome inválido!");
            return;
        }

        if (_modoAdicionar)
            TextBoxValue = Nome;

        Close(true);
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}