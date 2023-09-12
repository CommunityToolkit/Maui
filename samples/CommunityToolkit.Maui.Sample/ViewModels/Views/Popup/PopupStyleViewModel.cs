using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class PopupStyleViewModel : BaseViewModel
{
	static Page Page => Application.Current?.MainPage ?? throw new NullReferenceException();

	[RelayCommand]
	static void DisplayPopup1()
	{
		var popup = new ImplicitStylePopup();
		Page.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayPopup2()
	{
		var popup = new ExplicitStylePopup();
		Page.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayPopup3()
	{
		var popup = new DynamicStylePopup();
		Page.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayPopup4()
	{
		var popup = new ApplyToDerivedTypesPopup();
		Page.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayPopup5()
	{
		var popup = new StyleInheritancePopup();
		Page.ShowPopup(popup);
	}

	[RelayCommand]
	static void DisplayPopup6()
	{
		var popup = new StyleClassPopup();
		Page.ShowPopup(popup);
	}
}
