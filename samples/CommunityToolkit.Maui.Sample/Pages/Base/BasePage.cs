using System.Diagnostics;
using CommunityToolkit.Maui.Sample.ViewModels;

namespace CommunityToolkit.Maui.Sample.Pages;

public abstract class BasePage<TViewModel> : BasePage where TViewModel : BaseViewModel
{
	protected BasePage(TViewModel viewModel) : base(viewModel)
	{
	}

	public new TViewModel BindingContext => (TViewModel)base.BindingContext;
}

public abstract class BasePage : ContentPage
{
	protected BasePage(object? viewModel = null)
	{
		BindingContext = viewModel;

		BackgroundColor = (Color)(Application.Current?.Resources["AppBackgroundColor"] ?? throw new InvalidOperationException("Application.Current cannot be null"));

		Padding = Device.RuntimePlatform switch
		{
			// Work-around to ensure content doesn't get clipped by iOS Status Bar + Naviagtion Bar
			Device.iOS or Device.MacCatalyst => new Thickness(12, 108, 12, 12),
			_ => 12
		};

		if(string.IsNullOrWhiteSpace(Title))
		{
			Title = this.GetType().Name;
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