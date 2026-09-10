using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IntuitiveMedia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial bool AreControlsVisible { get; set; }

    // [RelayCommand]
    // public void ShowMyComponent()
    // {
    //     AreControlsVisible = true;
    // }
}
