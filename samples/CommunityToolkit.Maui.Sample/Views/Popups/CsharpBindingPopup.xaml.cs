using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class CsharpBindingPopup : Popup
{
	public CsharpBindingPopup(CsharpBindingPopupViewModel csharpBindingPopupViewModel)
	{
		InitializeComponent();
		BindingContext = csharpBindingPopupViewModel;
	}
}