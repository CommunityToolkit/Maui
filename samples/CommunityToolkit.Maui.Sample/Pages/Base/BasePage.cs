using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.Pages;

public abstract class BasePage<TViewModel> : BasePage where TViewModel : BaseViewModel
{
	public BasePage(TViewModel viewModel) : base(viewModel)
	{
		ViewModel = viewModel;
	}

	protected TViewModel ViewModel { get; }
}

public abstract class BasePage : ContentPage
{
	public BasePage(object? viewModel = null)
	{
		BindingContext = viewModel;
		Padding = 20;
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