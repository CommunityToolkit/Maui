using CommunityToolkit.Maui.Alerts;
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

	async void Button_Clicked(object? sender, EventArgs e)
	{
		await CloseAsync();
		await Toast.Make("Popup Dismissed By Button").Show();
	}
}