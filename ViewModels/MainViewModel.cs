using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IntuitiveMedia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public static implicit operator Window(MainViewModel v)
    {
        throw new NotImplementedException();
    }
}
