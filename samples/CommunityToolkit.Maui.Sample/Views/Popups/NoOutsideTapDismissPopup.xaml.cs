using CommunityToolkit.Maui.Alerts;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class NoOutsideTapDismissPopup : Maui.Views.Popup
{
	public NoOutsideTapDismissPopup()
	{
		InitializeComponent();
	}

	async void Button_Clicked(object? sender, EventArgs e)
	{
		await CloseAsync();
		await Toast.Make("Popup Dismissed By Button").Show();
	}
}