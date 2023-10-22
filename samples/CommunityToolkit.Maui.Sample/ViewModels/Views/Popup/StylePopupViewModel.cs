using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class StylePopupViewModel : BaseViewModel
{
	static Page MainPage => Shell.Current;

	[RelayCommand]
	static void DisplayImplicitStylePopup()
	{
		var popup = new ImplicitStylePopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayExplicitStylePopup()
	{
		var popup = new ExplicitStylePopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayDynamicStylePopup()
	{
		var popup = new DynamicStylePopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayApplyToDerivedTypesPopup()
	{
		var popup = new ApplyToDerivedTypesPopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayStyleInheritancePopup()
	{
		var popup = new StyleInheritancePopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayDynamicStyleInheritancePopup()
	{
		var popup = new DynamicStyleInheritancePopup();
		MainPage.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayStyleClassPopup()
	{
		var popup = new StyleClassPopup();
		MainPage.ShowPopup(popup);
	}
}