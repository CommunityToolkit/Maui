using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;
public sealed class PopupGalleryViewModel : BaseGalleryViewModel
{
	public PopupGalleryViewModel() 
		: base(new[]
		{
			new SectionModel(typeof(SimplePopup), "Simple Popup", Colors.Red, "Displays a basic popup centered on the screen"),
			new SectionModel(typeof(PopupPositionPage), "Custom Positioning Popup", Colors.Red, "Displays a basic popup anywhere on the screen using VerticalOptions and HorizontalOptions"),
			new SectionModel(typeof(ButtonPopup), "Popup With 1 Button", Colors.Red, "Displays a basic popup with a confirm button"),
			new SectionModel(typeof(MultipleButtonPopup), "Popup With Multiple Buttons", Colors.Red, "Displays a basic popup with a cancel and confirm button"),
			new SectionModel(typeof(NoLightDismissPopup), "Simple Popup Without Light Dismiss", Colors.Red, "Displays a basic popup but does not allow the user to close it if they tap outside of the popup. In other words the LightDismiss is set to false."),
			new SectionModel(typeof(ToggleSizePopup), "Toggle Size Popup", Colors.Red, "Displays a popup that can have it's size updated by pressing a button"),
			new SectionModel(typeof(TransparentPopup), "Transparent Popup", Colors.Red, "Displays a popup with a transparent background"),
			new SectionModel(typeof(PopupAnchorPage), "Anchor Popup", Colors.Red, "Popups can be anchored to other view's on the screen"),
			new SectionModel(typeof(OpenedEventSimplePopup), "Opened Event Popup", Colors.Red, "Popup with opened event"),
			new SectionModel(typeof(ReturnResultPopup), "Return Result Popup", Colors.Red, "A popup that returns a string message when dismissed"),
			new SectionModel(typeof(XamlBindingPopup), "Xaml Binding Popup", Colors.Red, "A simple popup that uses XAML BindingContext"),
			new SectionModel(typeof(CsharpBindingPopup), "C# Binding Popup", Colors.Red, "A simple popup that uses C# BindingContext")
		})
	{
		DisplayPopup = new AsyncRelayCommand<Type?>(OnDisplayPopup);
	}

	public ICommand DisplayPopup { get; }


	async Task OnDisplayPopup(Type? popupType)
	{
		var mainPage = Application.Current?.MainPage;
		if (popupType is null || mainPage is null)
		{
			return;
		}

		var view = (Element?)Activator.CreateInstance(popupType);

		if (view is Popup<string?> popup)
		{
			var result = await mainPage!.Navigation.ShowPopupAsync(popup);
			await mainPage.DisplayAlert("Popup Result", result, "OKAY");
		}
		else if (view is BasePopup basePopup)
		{
			mainPage.Navigation.ShowPopup(basePopup);
		}
		else if (view is Page page)
		{
			await mainPage.Navigation.PushAsync(page);
		}
	}
}
