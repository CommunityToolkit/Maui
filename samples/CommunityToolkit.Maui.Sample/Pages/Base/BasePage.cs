using System.Diagnostics;
using CommunityToolkit.Maui.Sample.ViewModels;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace CommunityToolkit.Maui.Sample.Pages;

public abstract class BasePage<TViewModel>(TViewModel viewModel, bool shouldUseSafeArea = true) : BasePage(viewModel, shouldUseSafeArea)
	where TViewModel : BaseViewModel
{
	public new TViewModel BindingContext => (TViewModel)base.BindingContext;
}

public abstract class BasePage : ContentPage
{
	protected BasePage(object? viewModel = null, bool shouldUseSafeArea = true)
	{
		BindingContext = viewModel;
		Padding = 12;

		On<iOS>().SetUseSafeArea(shouldUseSafeArea);

		if (string.IsNullOrWhiteSpace(Title))
		{
			Title = GetType().Name;
		}
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		Debug.WriteLine($"OnAppearing: {Title}");
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();

		Debug.WriteLine($"OnDisappearing: {Title}");
	}
}