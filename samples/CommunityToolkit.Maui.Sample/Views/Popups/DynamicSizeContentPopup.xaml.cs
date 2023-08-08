using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class DynamicSizeContentPopup : Popup
{
	public DynamicSizeContentPopup(DynamicSizeContentPopupViewModel dynamicSizeContentPopupViewModel)
	{
		InitializeComponent();
		BindingContext = dynamicSizeContentPopupViewModel;
	}
}