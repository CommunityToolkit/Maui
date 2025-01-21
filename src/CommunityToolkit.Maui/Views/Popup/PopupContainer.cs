using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace CommunityToolkit.Maui.Views;

partial class PopupContainer<T> : PopupContainer
{
	readonly TaskCompletionSource<PopupResult<T>> taskCompletionSource;

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