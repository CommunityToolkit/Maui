using static CommunityToolkit.Maui.Sample.Models.SectionModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Sample.ViewModels.Views.Popups;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;
public sealed class PopupGalleryViewModel : BaseGalleryViewModel
{
	public PopupGalleryViewModel() 
		: base(new[]
		{
			Create<PopupGalleryViewModel>(typeof(SimplePopup), "Simple Popup", Colors.Red, "Displays a basic popup centered on the screen"),
			Create<PopupPositionViewModel>(typeof(PopupPositionPage), "Custom Positioning Popup", Colors.Red, "Displays a basic popup anywhere on the screen using VerticalOptions and HorizontalOptions"),
			Create<PopupGalleryViewModel>(typeof(ButtonPopup), "Popup With 1 Button", Colors.Red, "Displays a basic popup with a confirm button"),
			Create<PopupGalleryViewModel>(typeof(MultipleButtonPopup), "Popup With Multiple Buttons", Colors.Red, "Displays a basic popup with a cancel and confirm button"),
			Create<PopupGalleryViewModel>(typeof(NoLightDismissPopup), "Simple Popup Without Light Dismiss", Colors.Red, "Displays a basic popup but does not allow the user to close it if they tap outside of the popup. In other words the LightDismiss is set to false."),
			Create<PopupGalleryViewModel>(typeof(ToggleSizePopup), "Toggle Size Popup", Colors.Red, "Displays a popup that can have it's size updated by pressing a button"),
			Create<PopupGalleryViewModel>(typeof(TransparentPopup), "Transparent Popup", Colors.Red, "Displays a popup with a transparent background"),
			Create<PopupAnchorViewModel>(typeof(PopupAnchorPage), "Anchor Popup", Colors.Red, "Popups can be anchored to other view's on the screen"),
			Create<PopupGalleryViewModel>(typeof(OpenedEventSimplePopup), "Opened Event Popup", Colors.Red, "Popup with opened event"),
			Create<PopupGalleryViewModel>(typeof(ReturnResultPopup), "Return Result Popup", Colors.Red, "A popup that returns a string message when dismissed"),
			Create<XamlBindingPopupViewModel>(typeof(XamlBindingPopup), "Xaml Binding Popup", Colors.Red, "A simple popup that uses XAML BindingContext"),
			Create<CsharpBindingPopupViewModel>(typeof(CsharpBindingPopup), "C# Binding Popup", Colors.Red, "A simple popup that uses C# BindingContext")
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

		if (view is Popup popup)
		{
			var result = await mainPage!.Navigation.ShowPopupAsync(popup);
			if (result is string s)
			{
				await mainPage.DisplayAlert("Popup Result", s, "OKAY");
			}
		}
		else if (view is Page page)
		{
			await mainPage.Navigation.PushAsync(page);
		}
	}
}
