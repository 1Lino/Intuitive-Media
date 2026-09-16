using CommunityToolkit.Mvvm.ComponentModel;

namespace IntuitiveMedia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial bool AreControlsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsPointerOverControls { get; set; }

    [ObservableProperty]
    public partial bool IsVideoDrawerOn { get; set; }
}
