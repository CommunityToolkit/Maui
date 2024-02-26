using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class ReturnResultPopup : Popup
{
	public ReturnResultPopup(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();

		Size = popupSizeConstants.Medium;
		ResultWhenUserTapsOutsideOfPopup = "User Tapped Outside of Popup";
	}

	async void Button_Clicked(object? sender, EventArgs e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await CloseAsync("Close button tapped", cts.Token);
	}
}