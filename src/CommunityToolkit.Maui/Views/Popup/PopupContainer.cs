using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace CommunityToolkit.Maui.Views;

partial class PopupContainer<T> : PopupContainer
{
	readonly TaskCompletionSource<PopupResult<T>> taskCompletionSource;

	public PopupContainer(Popup<T> content, TaskCompletionSource<PopupResult<T>> taskCompletionSource) : base(content, null)
	{
		this.taskCompletionSource = taskCompletionSource;
		
		Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
		On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);
	}

	public Task Close(PopupResult<T> result, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		
		taskCompletionSource.SetResult(result);
		return Navigation.PopModalAsync(false).WaitAsync(token);
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

		Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
		On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);
	}
	
	public bool CanBeDismissedByTappingOutsideOfPopup { get; internal set; }
	
	public Task Close(PopupResult result, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		
		taskCompletionSource?.SetResult(result);
		return Navigation.PopModalAsync(false).WaitAsync(token);
	}
	
	// Prevent the Android Back Button from dismissing the Popup if CanBeDismissedByTappingOutsideOfPopup is true
	protected override bool OnBackButtonPressed()
	{
		if (CanBeDismissedByTappingOutsideOfPopup)
		{
			return base.OnBackButtonPressed();
		}

		return true;
	}

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
}