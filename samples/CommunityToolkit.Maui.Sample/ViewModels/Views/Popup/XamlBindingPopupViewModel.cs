namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class XamlBindingPopupViewModel : BaseViewModel
{
	public string Title { get; } = "Xaml Binding Popup";

	public string Message { get; } = "This is a platform specific popup with a .NET MAUI View being rendered.\nThe behaviors of the popup will confirm to 100% as each platform implementation look and feel, but still allows you to use your .NET MAUI Controls.";
}