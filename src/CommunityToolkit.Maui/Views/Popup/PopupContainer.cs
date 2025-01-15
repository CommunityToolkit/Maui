using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace CommunityToolkit.Maui.Views;

partial class PopupContainer<T> : PopupContainer
{
	readonly TaskCompletionSource<PopupResult<T>> taskCompletionSource;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="content"></param>
	/// <param name="taskCompletionSource"></param>
	public PopupContainer(Popup<T> content, TaskCompletionSource<PopupResult<T>> taskCompletionSource) :base (content, null)
	{
		this.taskCompletionSource = taskCompletionSource;
		content.SetPopup(this);
		Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
		On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);
	}

	public async Task Close(PopupResult<T> result)
	{
		taskCompletionSource.SetResult(result);
		await Navigation.PopModalAsync().ConfigureAwait(false);
	}
}

partial class PopupContainer : ContentPage
{
	readonly Popup content;
	readonly TaskCompletionSource<PopupResult>? taskCompletionSource;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="content"></param>
	/// <param name="taskCompletionSource"></param>
	public PopupContainer(Popup content, TaskCompletionSource<PopupResult>? taskCompletionSource)
	{
		this.content = content;
		this.taskCompletionSource = taskCompletionSource;
		content.SetPopup(this);
		Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
		On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);
	}

	protected override bool OnBackButtonPressed()
	{
		if (CanBeDismissedByTappingOutsideOfPopup)
		{
			return base.OnBackButtonPressed();
		}

		return true;
	}

	/// <summary>
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping outside the Popup.
	/// </summary>
	/// <remarks>
	/// When true and the user taps outside the popup, it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	public bool CanBeDismissedByTappingOutsideOfPopup { get; internal set; }

	protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
	{
		content.NotifyPopupIsClosed();
		base.OnNavigatedFrom(args);
	}

	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);
		content.NotifyPopupIsOpened();
	}

	public async Task Close(PopupResult result)
	{
		taskCompletionSource?.SetResult(result);
		await Navigation.PopModalAsync().ConfigureAwait(false);
	}
}