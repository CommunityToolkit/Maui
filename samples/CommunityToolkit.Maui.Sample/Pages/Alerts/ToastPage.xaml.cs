using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;

namespace CommunityToolkit.Maui.Sample.Pages.Alerts;

public partial class ToastPage : BasePage<ToastViewModel>
{
	public ToastPage(ToastViewModel toastViewModel) : base(toastViewModel)
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