using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class CsharpBindingPopupViewModel : BaseViewModel
{
	[ObservableProperty]
	string title = "C# Binding Popup";

	[ObservableProperty]
	string message = "This message uses a ViewModel binding";

	internal void Load(string updatedMessage)
	{
		Message = updatedMessage;
	}
}