using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class ReturnResultPopup : Popup
{
	public ReturnResultPopup(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();

		Size = popupSizeConstants.Medium;
		ResultWhenUserTapsOutsideOfPopup = "User Tapped Outside of Popup";
	}

	void Button_Clicked(object? sender, EventArgs e) => Close("Close button tapped");
}