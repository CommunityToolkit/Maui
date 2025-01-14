using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace CommunityToolkit.Maui.Views;

partial class PopupContainer : ContentPage
{
	/// <summary>
	/// 
	/// </summary>
	public PopupContainer()
	{
		Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
		On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	protected override bool OnBackButtonPressed()
	{
		return !CanBeDismissedByTappingOutsideOfPopup;
	}

	/// <summary>
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping outside the Popup.
	/// </summary>
	/// <remarks>
	/// When true and the user taps outside the popup, it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	public bool CanBeDismissedByTappingOutsideOfPopup { get; internal set; }
	
	/// <summary>
	/// 
	/// </summary>
	protected sealed override void OnAppearing()
	{
		base.OnAppearing();
		if (Content is Popup popup)
		{
			popup.OnOpened?.Invoke();
		}
	}

	/// <summary>
	/// 
	/// </summary>
	protected sealed override void OnDisappearing()
	{
		if (Content is Popup popup)
		{
			popup.OnClosed?.Invoke();
		}

		base.OnDisappearing();
	}

	public void Close(PopupResult result)
	{
		
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="result"></param>
	public void Close<T>(PopupResult<T> result)
	{
		
	}
}