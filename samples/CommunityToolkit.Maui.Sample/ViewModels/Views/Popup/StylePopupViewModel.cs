using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class StylePopupViewModel : BaseViewModel
{
	static Page MainPage => Application.Current?.MainPage ?? throw new InvalidOperationException("MainPage cannot be null while app is running");

	[RelayCommand]
	static void DisplayPopup1()
	{
		var popup = new ImplicitStylePopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayPopup2()
	{
		var popup = new ExplicitStylePopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayPopup3()
	{
		var popup = new DynamicStylePopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayPopup4()
	{
		var popup = new ApplyToDerivedTypesPopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayPopup5()
	{
		var popup = new StyleInheritancePopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayPopup6()
	{
		var popup = new DynamicStyleInheritancePopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayPopup7()
	{
		var popup = new StyleClassPopup();
		MainPage.ShowPopup(popup);
	}
}
