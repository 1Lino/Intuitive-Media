using CommunityToolkit.Mvvm.ComponentModel;

namespace IntuitiveMedia.ViewModels;

// Este é apenas um modelo base de um ViewModel do Avalonia, e é utilizado caso os demais ViewModels possuam algo em comum, aí deve-se declarar estas propriedades comuns aqui. Do contrário, é possível ignorar isto aqui também e fazer com que cada ViewModel herde de ObservableObject, mas isto diminuiria a integração entre os diferentes ViewModels do projeto, mas isto pode ser interessante em projetos pequenos.
public abstract class ViewModelBase : ObservableObject
{
}
