using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Sample.Pages.Alerts;

public partial class ToastPage : BasePage
{
	public ToastPage()
	{
		InitializeComponent();
	}

	async void ShowToastButtonClicked(object? sender, EventArgs args)
	{
		var toast = Toast.Make("This is a default Toast.");
		await toast.Show();
	}

	async void ShowCustomToastButtonClicked(object? sender, EventArgs args)
	{
		var toast = Toast.Make("This is a big Toast.", ToastDuration.Long, 30d);
		await toast.Show();
	}
}