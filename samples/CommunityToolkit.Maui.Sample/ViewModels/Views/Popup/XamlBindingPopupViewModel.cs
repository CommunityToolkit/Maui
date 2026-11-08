namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class XamlBindingPopupViewModel : BaseViewModel
{
	public string Title { get; } = "Xaml Binding Popup";

	public string Message { get; } = "This popup's XAML sets a view model as its BindingContext and binds the title and this message to it.";
}