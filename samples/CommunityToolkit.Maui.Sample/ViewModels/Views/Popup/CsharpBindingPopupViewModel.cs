namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class CsharpBindingPopupViewModel : BaseViewModel
{
	public string Title { get; } = "C# Binding Popup";

	public string Message { get; private set; } = "";

	internal void Load(string message)
	{
		Message = message;
	}
}