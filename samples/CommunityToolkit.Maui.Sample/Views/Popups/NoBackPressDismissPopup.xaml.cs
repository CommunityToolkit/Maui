using CommunityToolkit.Maui.Alerts;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class NoBackPressDismissPopup : Maui.Views.Popup
{
	public NoBackPressDismissPopup()
	{
		InitializeComponent();
	}

	async void Button_Clicked(object? sender, EventArgs e)
	{
		await CloseAsync();
		await Toast.Make("Popup Dismissed By Button").Show();
	}
}