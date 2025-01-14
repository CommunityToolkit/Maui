using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class CsharpBindingPopup : ContentView
{
	public CsharpBindingPopup(CsharpBindingPopupViewModel csharpBindingPopupViewModel)
	{
		InitializeComponent();
		BindingContext = csharpBindingPopupViewModel;
	}
}