using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class StylePopupViewModel : BaseViewModel
{
	static Page MainPage => Shell.Current;

	[RelayCommand]
	static async Task DisplayImplicitStylePopup()
	{
		var popup = new ImplicitStylePopup();
		await MainPage.ShowPopupAsync(popup);
	}

	[RelayCommand]
	static async Task DisplayExplicitStylePopup()
	{
		var popup = new ExplicitStylePopup();
		await MainPage.ShowPopupAsync(popup, PopupOptions.Empty);
	}

	[RelayCommand]
	static async Task DisplayDynamicStylePopup()
	{
		var popup = new DynamicStylePopup();
		await MainPage.ShowPopupAsync(popup, PopupOptions.Empty);
	}

	[RelayCommand]
	static async Task DisplayApplyToDerivedTypesPopup()
	{
		var popup = new ApplyToDerivedTypesPopup();
		await MainPage.Navigation.ShowPopupAsync(popup, PopupOptions.Empty);
	}

	[RelayCommand]
	static async Task DisplayStyleInheritancePopup()
	{
		var popup = new StyleInheritancePopup();
		await MainPage.ShowPopupAsync(popup, PopupOptions.Empty);
	}

	[RelayCommand]
	static async Task DisplayDynamicStyleInheritancePopup()
	{
		var popup = new DynamicStyleInheritancePopup();
		await MainPage.ShowPopupAsync(popup, PopupOptions.Empty);
	}

	[RelayCommand]
	static async Task DisplayStyleClassPopup()
	{
		var popup = new StyleClassPopup();
		await MainPage.ShowPopupAsync(popup, PopupOptions.Empty);
	}
}