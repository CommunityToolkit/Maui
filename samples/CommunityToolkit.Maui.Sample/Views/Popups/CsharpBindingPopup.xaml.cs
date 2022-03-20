using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class CsharpBindingPopup : Popup
{
	public CsharpBindingPopup(PopupSizeConstants popupSizeConstants, CsharpBindingPopupViewModel csharpBindingPopupViewModel)
	{
		InitializeComponent();
		BindingContext = csharpBindingPopupViewModel;

		Size = popupSizeConstants.Large;
	}
}