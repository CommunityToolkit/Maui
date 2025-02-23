using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class NoOutsideTapDismissPopup : Maui.Views.Popup
{
	public NoOutsideTapDismissPopup()
	{
		InitializeComponent();
	}

	async void Button_Clicked(object? sender, EventArgs e)
	{
		await Close();
		await Toast.Make("Popup Dismissed By Button").Show();
	}
}