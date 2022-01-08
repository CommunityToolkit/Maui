using System.Reflection;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Sample.Pages.Alerts;

public partial class ToastPage : BasePage
{
	IToast? toast;

	public ToastPage()
	{
		InitializeComponent();
	}

	async void ShowToastButtonClicked(object? sender, EventArgs args)
	{
		toast = Toast.Make("This is a Toast.", ToastDuration.Long);
		await toast.Show();
	}
}