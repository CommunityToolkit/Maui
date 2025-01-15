using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class CsharpBindingPopup : Popup
{
	public CsharpBindingPopup(CsharpBindingPopupViewModel csharpBindingPopupViewModel)
	{
		InitializeComponent();
		BindingContext = csharpBindingPopupViewModel;
		OnOpened += (s, e) =>
			csharpBindingPopupViewModel.Load(
				"This is a platform specific popup with a .NET MAUI View being rendered. The behaviors of the popup will confirm to 100% this platform look and feel, but still allows you to use your .NET MAUI Controls.");
	}
}