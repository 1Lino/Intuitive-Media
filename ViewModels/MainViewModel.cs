using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IntuitiveMedia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial bool AreControlsVisible { get; set; }

    // TODO: encontrar uma forma de os controles acessarem essa propriedade para alterar ela. Quando o usuário estiver com o mouse dentro do controle, essa propriedade deve se tornar "false";
    [ObservableProperty]
    public partial bool IsPointerOverControls { get; set; }
}
