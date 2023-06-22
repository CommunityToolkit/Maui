using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class XamlBindingPopup : Popup
{
	public XamlBindingPopup(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();

		Size = popupSizeConstants.Large;
	}
}