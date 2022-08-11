using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

[INotifyPropertyChanged]
public sealed partial class XamlBindingPopupViewModel
{
	public string Title { get; } = "Xaml Binding Popup";

	public string Message { get; } = "This is a platform specific popup with a .NET MAUI View being rendered. The behaviors of the popup will confirm to 100% as each platform implementation look and feel, but still allows you to use your .NET MAUI Controls.";
}