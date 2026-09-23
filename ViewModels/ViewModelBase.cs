using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IntuitiveMedia.ViewModels;

// Este é apenas um modelo base de um ViewModel do Avalonia, e é utilizado caso os demais ViewModels possuam algo em comum, aí deve-se declarar estas propriedades comuns aqui. Do contrário, é possível ignorar isto aqui também e fazer com que cada ViewModel herde de ObservableObject, mas isto diminuiria a integração entre os diferentes ViewModels do projeto, mas isto pode ser interessante em projetos pequenos.
public abstract class ViewModelBase : ObservableObject, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
