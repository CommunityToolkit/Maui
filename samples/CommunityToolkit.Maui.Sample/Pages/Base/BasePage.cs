using System.Diagnostics;
using CommunityToolkit.Maui.Sample.ViewModels;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace CommunityToolkit.Maui.Sample.Pages;

public abstract class BasePage<TViewModel> : BasePage where TViewModel : BaseViewModel
{
	protected BasePage(TViewModel viewModel) : base(viewModel)
	{
	}
}

public abstract class BasePage : ContentPage
{
	protected BasePage(object? viewModel = null)
	{
		BindingContext = viewModel;
		Padding = 12;
		On<iOS>().SetLargeTitleDisplay(LargeTitleDisplayMode.Always);
	}

	protected override void OnAppearing()
	{
		Debug.WriteLine($"OnAppearing: {this}");
	}

	protected override void OnDisappearing()
	{
		Debug.WriteLine($"OnDisappearing: {this}");
	}
}