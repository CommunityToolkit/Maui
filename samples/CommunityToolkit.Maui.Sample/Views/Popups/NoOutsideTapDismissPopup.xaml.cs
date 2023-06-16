using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class NoOutsideTapDismissPopup : Popup
{
	public NoOutsideTapDismissPopup(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();

		Size = popupSizeConstants.Medium;
	}

	void Button_Clicked(object? sender, System.EventArgs e) => Close();
}