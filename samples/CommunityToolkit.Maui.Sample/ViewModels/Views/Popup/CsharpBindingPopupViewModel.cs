namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class CsharpBindingPopupViewModel : BaseViewModel
{
	public string Title { get; } = "C# Binding Popup";

	public string Message { get; } = "This is a platform specific popup with a .NET MAUI View being rendered. The behaviors of the popup will confirm to 100% this platform look and feel, but still allows you to use your .NET MAUI Controls.";
}