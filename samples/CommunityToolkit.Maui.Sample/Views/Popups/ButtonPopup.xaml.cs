using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class ButtonPopup : Popup
{
	public ButtonPopup(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}

	async void Button_Clicked(object? sender, EventArgs e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await CloseAsync(token: cts.Token);
	}
}