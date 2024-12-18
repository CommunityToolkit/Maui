using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class CsharpBindingPopupViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string Title { get; set; } = "C# Binding Popup";

	[ObservableProperty]
	public partial string Message { get; set; } = "This message uses a ViewModel binding";

	internal void Load(string updatedMessage)
	{
		Message = updatedMessage;
	}
}