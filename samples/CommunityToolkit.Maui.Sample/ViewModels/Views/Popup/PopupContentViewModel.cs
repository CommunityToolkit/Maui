using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class PopupContentViewModel : BaseViewModel
{
    [ObservableProperty]
	string message = "";

    internal void SetMessage(string text) => this.Message = text;
}