using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using NavigationPage = Microsoft.Maui.Controls.NavigationPage;
using Page = Microsoft.Maui.Controls.Page;

namespace CommunityToolkit.Maui.Views;

sealed partial class PopupPage<T>(Popup<T> popup, IPopupOptions? popupOptions)
	: PopupPage(popup, popupOptions)
{
	public PopupPage(View view, IPopupOptions? popupOptions)
		: this(view as Popup<T> ?? CreatePopupFromView<Popup<T>>(view), popupOptions)
	{
	}

	public Task CloseAsync(PopupResult<T> result, CancellationToken token = default) => base.CloseAsync(result, token);
}

partial class PopupPage : ContentPage, IQueryAttributable
{
	/// <summary>
	/// Used by all <c>ShowPopup</c> and <c>ClosePopup</c> flows to ensure navigation operations for <see cref="PopupPage"/>
	/// are queued and never run concurrently, avoiding race conditions with the .NET MAUI navigation stack.
	/// (e.g. <see cref="INavigation.PushModalAsync(Page, bool)"/>, <see cref="INavigation.PopModalAsync(bool)"/>, and <see cref="Shell.GoToAsync(ShellNavigationState, IDictionary{string,object})"/>).
	/// </summary>
	static readonly SemaphoreSlim navigationSemaphoreSlim = new(1, 1);

	readonly Popup popup;
	readonly IPopupOptions popupOptions;
	readonly Command tapOutsideOfPopupCommand;

	NavigationPage? navigationPageObservedForDismissal;
	Window? windowObservedForDismissal;
	bool hasNotifiedPopupIsOpened;
	bool isCloseAsyncRunning;
	bool hasRaisedPopupClosed;

	public PopupPage(View view, IPopupOptions? popupOptions)
		: this(view as Popup ?? CreatePopupFromView<Popup>(view), popupOptions)
	{
		ArgumentNullException.ThrowIfNull(view);
	}

	public PopupPage(Popup popup, IPopupOptions? popupOptions)
	{
		ArgumentNullException.ThrowIfNull(popup);

		Options.DefaultPopupSettings.OnInitializing?.Invoke(popup);

		this.popup = popup;
		this.popupOptions = popupOptions ??= Options.DefaultPopupOptionsSettings;

		tapOutsideOfPopupCommand = new Command(async () =>
		{
			popupOptions.OnTappingOutsideOfPopup?.Invoke();

			try
			{
				await CloseAsync(new PopupResult(true));
			}
			catch (InvalidPopupOperationException)
			{
				// An earlier tap, the back button or navigation already closed this popup, or another page was shown above it while the tap waited; the command runs as async void, so an exception here would crash the app
			}
			catch (OperationCanceledException)
			{
				// The tap passes no token, so the pop was canceled: by the app (in Window.ModalPopping, Application.ModalPopping or Shell.Navigating), or by .NET MAUI (its NavigationPage on iOS and Mac Catalyst cancels a PopAsync that waited for a navigation that failed).
				// The popup is still open and a later tap can close it; the command runs as async void, so an exception here would crash the app
			}
		}, () => GetCanBeDismissedByTappingOutsideOfPopup(popup, popupOptions));

		var popupPageLayout = new PopupPageLayout(popup, popupOptions, () => TryExecuteTapOutsideOfPopupCommand());
		base.Content = popupPageLayout;

		popup.PropertyChanged += HandlePopupPropertyChanged;
		if (popupOptions is BindableObject bindablePopupOptions)
		{
			bindablePopupOptions.PropertyChanged += HandlePopupOptionsPropertyChanged;
		}

		this.SetBinding(BindingContextProperty, static (Popup x) => x.BindingContext, source: popup, mode: BindingMode.OneWay);
		this.SetBinding(BackgroundColorProperty, static (IPopupOptions options) => options.PageOverlayColor, source: popupOptions, mode: BindingMode.OneWay);

		Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
		On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);
		NavigationPage.SetHasNavigationBar(this, false);
	}

	public event EventHandler<IPopupResult>? PopupClosed;

	// Prevent Content from being set by external class
	// Casts `PopupPage.Content` to return typeof(PopupPageLayout)
	internal new PopupPageLayout Content => (PopupPageLayout)base.Content;

	public async Task ShowAsync(INavigation navigation, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(navigation);
		await navigationSemaphoreSlim.WaitAsync(token);

		try
		{
			token.ThrowIfCancellationRequested();

			TryInheritHideSoftInputFromPresentingPage(GetVisiblePage(navigation));

			await navigation.PushModalAsync(this, false);
			ThrowIfTheAppCanceledThePush(navigation);
			ObserveDismissalByNavigationIfNotClosed();
		}
		finally
		{
			navigationSemaphoreSlim.Release();
		}
	}

	public async Task ShowAsync(Shell shell, string shellRoute, IDictionary<string, object>? shellParameters = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(shell);
		ArgumentException.ThrowIfNullOrEmpty(shellRoute);
		await navigationSemaphoreSlim.WaitAsync(token);

		try
		{
			token.ThrowIfCancellationRequested();

			TryInheritHideSoftInputFromPresentingPage(shell.Navigation.ModalStack.Count > 0
														? GetVisiblePage(shell.Navigation)
														: shell.CurrentPage);

			if (shellParameters is null)
			{
				await shell.GoToAsync(shellRoute);
			}
			else
			{
				await shell.GoToAsync(shellRoute, shellParameters);
			}

			ThrowIfTheAppCanceledThePush(shell.Navigation);
			ObserveDismissalByNavigationIfNotClosed();
		}
		finally
		{
			navigationSemaphoreSlim.Release();
		}
	}

	public async Task CloseAsync(PopupResult result, CancellationToken token = default)
	{
		// We first call `.ThrowIfCancellationRequested()` to ensure we don't throw one of the `InvalidOperationException`s (below) if the `CancellationToken` has already been canceled.
		// This ensures we throw the correct `OperationCanceledException` in the rare scenario where a developer cancels the token, then manually calls `Navigation.PopModalAsync()` before calling `Popup.CloseAsync()`
		// It may feel a bit redundant, given that we again call `ThrowIfCancellationRequested` later in this method, however, this ensures we propagate the correct Exception to the developer.
		token.ThrowIfCancellationRequested();

		var navigationPageOnModalStackContainingPopupPage = GetContainerAndThrowIfNotOnTopOfModalStack();

		var popupConfirmedPoppedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var parentWindow = GetParentWindow();
		parentWindow.ModalPopped += HandleModalPagePopped;
		NavigatedFrom += HandleNavigatedFrom;

		try
		{
			// navigationSemaphoreSlim must be in the outer try/finally block to ensure we unsubscribe ModalPopped and NavigatedFrom events when CancellationToken is canceled
			// When CancellationToken is canceled during navigationSemaphoreSlim.WaitAsync(), the SemaphoreSlim is never acquired. We only want to release navigationSemaphoreSlim in a finally block if it has been acquired (i.e. after navigationSemaphoreSlim.WaitAsync executes successfully)
			// The inner try/finally block ensures we release navigationSemaphoreSlim after its acquisition if any subsequent line of code throws an exception or completes successfully
			await navigationSemaphoreSlim.WaitAsync(token);

			try
			{
				// Navigation removed this PopupPage while CloseAsync waited for the semaphore, and that removal already reported the result.
				// Continuing would pop whichever page is on top of the navigation stack now.
				if (hasRaisedPopupClosed)
				{
					throw new PopupNotFoundException();
				}

				// Another page (e.g. a popup whose ShowAsync waited for the semaphore before this CloseAsync) may have been pushed above this PopupPage while CloseAsync waited.
				// Popping now would remove that page, and the confirmation that this PopupPage was popped would never come, leaving the semaphore held.
				navigationPageOnModalStackContainingPopupPage = GetContainerAndThrowIfNotOnTopOfModalStack();

				// CloseAsync reports the result itself, so the pop it causes must not be reported again as a dismissal by navigation.
				// Set only after the semaphore is acquired, otherwise a removal that happens while CloseAsync waits would be ignored.
				isCloseAsyncRunning = true;

				try
				{
					// With a Shell as the root page, .NET MAUI pushes a popup that is shown on top of a modal NavigationPage onto the navigation stack of that NavigationPage.
					// PopModalAsync would remove the whole NavigationPage, and never report the PopupPage as popped.
					if (navigationPageOnModalStackContainingPopupPage is NavigationPage { Navigation.NavigationStack.Count: > 1 } navigationPageContainingPopupPage)
					{
						await navigationPageContainingPopupPage.Navigation.PopAsync(false);

						// Popping within a NavigationPage raises neither ModalPopped nor a NavigatedFrom that CloseAsync accepts when the page below is another PopupPage
						popupConfirmedPoppedTcs.TrySetResult();
					}
					else
					{
						await Navigation.PopModalAsync(false);
					}
				}
				catch (Exception ex) when (!(ex is OperationCanceledException or InvalidPopupOperationException))
				{
					// MAUI's Shell throws an ArgumentException from its route resolver
					// (ShellUriHandler.GetNavigationRequest) when a PopupPage is popped from a
					// Shell in certain navigation states — a framework bug the Popup can't
					// prevent. Confirm the pop locally so the `await` below doesn't hang, then
					// keep closing; letting this escape would crash an async-void caller of CloseAsync.
					popupConfirmedPoppedTcs.TrySetResult();
				}

				// .NET MAUI returns without popping, and without an exception, when the app cancels the pop in Window.ModalPopping, Application.ModalPopping or Shell.Navigating.
				// ModalPopped and NavigatedFrom never come then: waiting for them would hold the static semaphore forever and block every popup of the app
				if (!popupConfirmedPoppedTcs.Task.IsCompleted && IsOnModalStack(parentWindow))
				{
					throw new OperationCanceledException("The Popup was not closed because the app canceled the navigation that removes it, for example in a Window.ModalPopping, Application.ModalPopping or Shell.Navigating handler. The Popup is still open.");
				}

				ReleasePopupResources();

				// Wait for MAUI to confirm the PopupPage has been popped before invoking the PopupClosed event and notifying the Popup that it may invoke its `Popup.Closed` event.
				// This guarantees the Popup has been removed from MAUI's ModalStack
				await popupConfirmedPoppedTcs.Task;
			}
			finally
			{
				// Cleared by the holder of the semaphore only: a concurrent CloseAsync that is canceled while it waits never set the flag, and must not clear the flag of the CloseAsync that is popping this page.
				// Nothing awaits between here and RaisePopupClosed, so no navigation event can be mistaken for a dismissal in between.
				isCloseAsyncRunning = false;
				navigationSemaphoreSlim.Release();
			}

			// Raised after navigationSemaphoreSlim is released because PopupClosed completes ShowPopupAsync, whose caller may show another popup right away
			RaisePopupClosed(result);
		}
		finally
		{
			parentWindow.ModalPopped -= HandleModalPagePopped;
			NavigatedFrom -= HandleNavigatedFrom;
		}

		void HandleModalPagePopped(object? sender, ModalPoppedEventArgs e)
		{
			// A PopupPage that is the only page of a modal NavigationPage is popped together with it, and ModalPopped reports the NavigationPage
			if (e.Modal == this || (e.Modal is IPageContainer<Page> container && container.CurrentPage == this))
			{
				popupConfirmedPoppedTcs.TrySetResult();
			}
		}

		void HandleNavigatedFrom(object? sender, NavigatedFromEventArgs e)
		{
			if (e.IsDestinationPageACommunityToolkitPopupPage())
			{
				// Ignore transitions where the destination is another PopupPage
				// that means this popup is still active in popup-navigation flows.
				return;
			}

			if (navigationPageOnModalStackContainingPopupPage?.CurrentPage == this)
			{
				// When `navigationPageOnModalStackContainingPopupPage.CurrentPage == this` is true,
				// this PopupPage is still the active page in the custom IPageContainer.
				// In other words, MAUI has not yet popped it off the ModalStack.
				return;
			}

			popupConfirmedPoppedTcs.TrySetResult();
		}
	}

	void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (popup is IQueryAttributable popupIQueryAttributable)
		{
			popupIQueryAttributable.ApplyQueryAttributes(query);
		}

		if (popup.Content is IQueryAttributable popupContentIQueryAttributable)
		{
			popupContentIQueryAttributable.ApplyQueryAttributes(query);
		}
	}

	internal bool TryExecuteTapOutsideOfPopupCommand()
	{
		if (!tapOutsideOfPopupCommand.CanExecute(null))
		{
			return false;
		}

		tapOutsideOfPopupCommand.Execute(null);
		return true;
	}

	protected static T CreatePopupFromView<T>(in View view) where T : Popup, new()
	{
		ArgumentNullException.ThrowIfNull(view);

		var popup = new T
		{
			Content = view
		};
		popup.SetBinding(BackgroundProperty, static (View view) => view.Background, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(BindingContextProperty, static (View view) => view.BindingContext, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(BackgroundColorProperty, static (View view) => view.BackgroundColor, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(Popup.MarginProperty, static (View view) => view.Margin, source: view, mode: BindingMode.OneWay, converter: new MarginConverter());
		popup.SetBinding(Popup.VerticalOptionsProperty, static (View view) => view.VerticalOptions, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(Popup.HorizontalOptionsProperty, static (View view) => view.HorizontalOptions, source: view, mode: BindingMode.OneWay);

		if (view is IPaddingElement paddingElement)
		{
			popup.SetBinding(Popup.PaddingProperty, static (IPaddingElement paddingElement) => paddingElement.Padding, source: paddingElement, mode: BindingMode.OneWay, converter: new PaddingConverter());
		}

		return popup;
	}

	protected override bool OnBackButtonPressed()
	{
		TryExecuteTapOutsideOfPopupCommand();

		// Always return true to let the Android Operating System know that we are manually handling the Navigation request from the Android Back Button
		return true;
	}

	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		// .NET MAUI raises NavigatedTo again when a page above this PopupPage on the ModalStack (e.g. another Popup) is popped
		// A new PopupPage is created every time a Popup is shown, so notify the Popup only the first time this PopupPage is navigated to
		// .NET MAUI also raised NavigatedFrom on this PopupPage when that page was pushed, which tore down the platform setup of the Popup (e.g. the iOS keyboard observers)
		// Set it up again, without raising Opened again
		if (hasNotifiedPopupIsOpened)
		{
			popup.SetupPlatform();
			return;
		}

		hasNotifiedPopupIsOpened = true;
		ObserveDismissalByNavigation();
		popup.NotifyPopupIsOpened();
	}

	protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
	{
		base.OnNavigatedFrom(args);
		popup.TeardownPlatform();
	}

	// Only dismiss when a user taps outside Popup when **both** Popup.CanBeDismissedByTappingOutsideOfPopup and PopupOptions.CanBeDismissedByTappingOutsideOfPopup are true
	// If either value is false, do not dismiss Popup
	static bool GetCanBeDismissedByTappingOutsideOfPopup(in Popup popup, in IPopupOptions popupOptions) => popup.CanBeDismissedByTappingOutsideOfPopup & popupOptions.CanBeDismissedByTappingOutsideOfPopup;

	// Called again once CloseAsync holds the semaphore, because the ModalStack can change while CloseAsync waits.
	IPageContainer<Page>? GetContainerAndThrowIfNotOnTopOfModalStack()
	{
		// Handle edge case where a Popup was pushed inside a custom IPageContainer (e.g. a NavigationPage) on the Modal Stack
		var navigationPageOnModalStackContainingPopupPage = Navigation.ModalStack.OfType<IPageContainer<Page>>().LastOrDefault();
		if (navigationPageOnModalStackContainingPopupPage is not null && navigationPageOnModalStackContainingPopupPage.CurrentPage is not PopupPage)
		{
			navigationPageOnModalStackContainingPopupPage = null;
		}

		var popupPageToClose = navigationPageOnModalStackContainingPopupPage?.CurrentPage as PopupPage
							   ?? Navigation.ModalStack.OfType<PopupPage>().LastOrDefault()
							   ?? throw new PopupNotFoundException();

		// PopModalAsync will pop the last (top) page from the ModalStack
		// Ensure that the PopupPage the user is attempting to close is the last (top) page on the Modal stack before calling Navigation.PopModalAsync
		switch (Navigation.ModalStack[^1])
		{
			// Handle the edge case where the visible modal page is a navigation page containing a Popup that is not the Popup to be closed
			case IPageContainer<Page> { CurrentPage: PopupPage visiblePopupPageInCustomPageContainer } when visiblePopupPageInCustomPageContainer.Content != Content:
				throw new PopupBlockedException(visiblePopupPageInCustomPageContainer);

			// Handle edge case where the top of the modal stack is an IPageContainer whose CurrentPage is NOT a PopupPage
			// (e.g. a modal NavigationPage pushed after showing a popup).
			case IPageContainer<Page> { CurrentPage: not PopupPage }:
				throw new PopupBlockedException(Navigation.ModalStack[^1]);

			// Handle edge case where the visible modal page is not the Popup to be closed
			case ContentPage currentVisibleModalPage when currentVisibleModalPage.Content != Content:
				throw new PopupBlockedException(currentVisibleModalPage);
		}

		if (popupPageToClose.Content != Content)
		{
			throw new PopupBlockedException(popupPageToClose);
		}

		return navigationPageOnModalStackContainingPopupPage;
	}

	// Static PopupOptions (e.g. PopupOptions.Empty) outlive the PopupPage, so every path that closes the popup must unsubscribe from them; the -= calls are idempotent
	void ReleasePopupResources()
	{
		Content.TapGestureGestureOverlay.GestureRecognizers.Clear();
		popup.PropertyChanged -= HandlePopupPropertyChanged;
		if (popupOptions is BindableObject bindablePopupOptions)
		{
			bindablePopupOptions.PropertyChanged -= HandlePopupOptionsPropertyChanged;
		}
	}

	// Claims the right to report the result; the first caller wins, so PopupClosed and Popup.Closed are raised once whichever path closes the popup
	bool TryMarkPopupClosed()
	{
		if (hasRaisedPopupClosed)
		{
			return false;
		}

		hasRaisedPopupClosed = true;
		StopObservingDismissalByNavigation();
		return true;
	}

	void InvokePopupClosed(IPopupResult result)
	{
		PopupClosed?.Invoke(this, result);
		popup.NotifyPopupIsClosed();
	}

	void RaisePopupClosed(IPopupResult result)
	{
		if (TryMarkPopupClosed())
		{
			InvokePopupClosed(result);
		}
	}

	// Navigation removed the PopupPage without CloseAsync. Outside a Shell, .NET MAUI raises the NavigatedFrom and NavigatedTo of the removal after Window.ModalPopped, for the page on top by then,
	// so the result is raised through the Dispatcher: a Closed handler that shows another popup must not run before .NET MAUI has finished the removal
	void RaisePopupClosedForDismissalByNavigation()
	{
		if (!TryMarkPopupClosed())
		{
			return;
		}

		ReleasePopupResources();

		var result = new PopupResult(true);
		Dispatcher.Dispatch(() => InvokePopupClosed(result));
	}

	// A Window without a handler makes a Shell raise no NavigatedTo on the pages it pushes, so OnNavigatedTo alone would never observe a removal that follows the push.
	// ObserveDismissalByNavigation unsubscribes first, so observing here and in OnNavigatedTo, in either order, subscribes once
	void ObserveDismissalByNavigationIfNotClosed()
	{
		if (!hasRaisedPopupClosed)
		{
			ObserveDismissalByNavigation();
		}
	}

	// A PopupPage that navigation removes without CloseAsync (e.g. Shell popping the whole modal stack, or PopModalAsync called by other code) never reaches the end of CloseAsync,
	// leaving the task of ShowPopupAsync pending forever and Popup.Closed never raised
	void ObserveDismissalByNavigation()
	{
		StopObservingDismissalByNavigation();

		// A PopupPage that is not hosted by a Window (e.g. raised NavigatedTo directly) cannot be removed by navigation
		if (Window is null)
		{
			return;
		}

		windowObservedForDismissal = Window;
		windowObservedForDismissal.ModalPopped += HandleModalPoppedWithoutCloseAsync;
		windowObservedForDismissal.PropertyChanged += HandleWindowPropertyChangedWithoutCloseAsync;

		// Removing a page from a NavigationPage (PopAsync, PopToRootAsync, RemovePage, or Shell popping it) raises neither ModalPopped nor PropertyChanged of the Window
		if (Parent is NavigationPage containingNavigationPage)
		{
			navigationPageObservedForDismissal = containingNavigationPage;
			containingNavigationPage.ChildRemoved += HandleChildRemovedWithoutCloseAsync;
		}
	}

	void StopObservingDismissalByNavigation()
	{
		if (windowObservedForDismissal is not null)
		{
			windowObservedForDismissal.ModalPopped -= HandleModalPoppedWithoutCloseAsync;
			windowObservedForDismissal.PropertyChanged -= HandleWindowPropertyChangedWithoutCloseAsync;
			windowObservedForDismissal = null;
		}

		if (navigationPageObservedForDismissal is not null)
		{
			navigationPageObservedForDismissal.ChildRemoved -= HandleChildRemovedWithoutCloseAsync;
			navigationPageObservedForDismissal = null;
		}
	}

	void HandleModalPoppedWithoutCloseAsync(object? sender, ModalPoppedEventArgs e)
	{
		if (isCloseAsyncRunning)
		{
			return;
		}

		// A popup in a custom IPageContainer (e.g. a NavigationPage) is popped together with its container
		var wasThisPopupPagePopped = e.Modal == this
			|| (e.Modal is IPageContainer<Page> container && container.CurrentPage == this)
			|| (e.Modal is NavigationPage navigationPage && navigationPage.Navigation.NavigationStack.Contains(this));

		if (wasThisPopupPagePopped)
		{
			RaisePopupClosedForDismissalByNavigation();
		}
	}

	void HandleChildRemovedWithoutCloseAsync(object? sender, ElementEventArgs e)
	{
		if (!isCloseAsyncRunning && e.Element == this)
		{
			RaisePopupClosedForDismissalByNavigation();
		}
	}

	// Replacing Window.Page drops the whole modal stack without raising ModalPopped
	void HandleWindowPropertyChangedWithoutCloseAsync(object? sender, PropertyChangedEventArgs e)
	{
		if (isCloseAsyncRunning
			|| e.PropertyName != nameof(Window.Page)
			|| sender is not Window window)
		{
			return;
		}

		if (!IsOnModalStack(window))
		{
			RaisePopupClosedForDismissalByNavigation();
		}
	}

	// A PopupPage that is the only page of a modal NavigationPage is on the ModalStack through that NavigationPage
	bool IsOnModalStack(Window window) => window.Navigation.ModalStack.Any(modalPage => modalPage == this
		|| (modalPage is IPageContainer<Page> container && container.CurrentPage == this)
		|| (modalPage is NavigationPage navigationPage && navigationPage.Navigation.NavigationStack.Contains(this)));

	// .NET MAUI's Shell returns without pushing, and without an exception, when a Shell.Navigating handler cancels the navigation.
	// Nothing could close a PopupPage that was never pushed, so ShowPopupAsync would wait forever, and a static IPopupOptions (e.g. PopupOptions.Empty) would keep this PopupPage alive.
	// A page that is not in a Window yet also leaves the PopupPage without a Parent, but keeps it on its ModalStack and pushes it once the page is attached: that push was not canceled
	void ThrowIfTheAppCanceledThePush(INavigation navigation)
	{
		if (Parent is null && !hasRaisedPopupClosed && !navigation.ModalStack.Contains(this))
		{
			ReleasePopupResources();
			throw new OperationCanceledException("The Popup was not shown because the app canceled the navigation that displays it, for example in a Shell.Navigating handler.");
		}
	}

	bool TryInheritHideSoftInputFromPresentingPage(in Page? presentingPage)
	{
		// HideSoftInputOnTapped is only declared on ContentPage, so guard for that type before reading the value.
		if (presentingPage is not ContentPage contentPage)
		{
			return false;
		}

		HideSoftInputOnTapped = contentPage.HideSoftInputOnTapped;
		return true;
	}

	void HandlePopupOptionsPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(IPopupOptions.CanBeDismissedByTappingOutsideOfPopup))
		{
			tapOutsideOfPopupCommand.ChangeCanExecute();
		}
	}

	void HandlePopupPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == Popup.CanBeDismissedByTappingOutsideOfPopupProperty.PropertyName)
		{
			tapOutsideOfPopupCommand.ChangeCanExecute();
		}
	}

	static Page? GetVisiblePage(INavigation navigation)
	{
		var page = navigation.ModalStack?.LastOrDefault()
			?? navigation.NavigationStack?.LastOrDefault();

		if (page is null && Microsoft.Maui.Controls.Application.Current?.Windows is { Count: > 0 } windows)
		{
			page = windows
				.FirstOrDefault(window =>
					ReferenceEquals(window.Navigation, navigation)
					|| (window.Page is Page rootPage && ReferenceEquals(rootPage.Navigation, navigation)))
				?.Page
				?? windows.LastOrDefault()?.Page;
		}

		while (page is not null)
		{
			switch (page)
			{
				case Shell shell:
					page = shell.CurrentPage;
					break;
				case Microsoft.Maui.Controls.FlyoutPage flyout:
					page = flyout.Detail;
					break;
				case IPageContainer<Page> container:
					page = container.CurrentPage;
					break;
				default:
					return page;
			}
		}

		return null;
	}

	internal sealed partial class PopupGestureOverlay : BoxView
	{
		public PopupGestureOverlay()
		{
			BackgroundColor = Colors.Transparent;
			Background = Brush.Transparent;

#if ANDROID
			// .NET MAUI configures the native view for its gestures again, and makes it focusable again, whenever the gesture recognizers
			// change while the view has a handler. MAUI's own subscription (made in the constructor of View) runs first.
			if (GestureRecognizers is System.Collections.Specialized.INotifyCollectionChanged gestureRecognizers)
			{
				gestureRecognizers.CollectionChanged += (_, _) => HideNativeViewsFromAccessibility();
			}
#endif
		}

#if ANDROID
		// .NET MAUI makes the view hosting a TapGestureRecognizer Focusable on Android, causing TalkBack to announce this
		// transparent overlay as an "unlabeled" element. AutomationProperties only affect Handler.PlatformView, but the
		// gesture is attached to the ContainerView (WrapperView) when present, so both native views must be hidden.
		// OnHandlerChanged runs after the HandlerChanged event, i.e. after the GestureManager has configured the native view.
		protected override void OnHandlerChanged()
		{
			base.OnHandlerChanged();

			HideNativeViewsFromAccessibility();
		}

		static void HideFromAccessibility(Android.Views.View? view)
		{
			if (view is null)
			{
				return;
			}

			view.ImportantForAccessibility = Android.Views.ImportantForAccessibility.NoHideDescendants;
			view.Focusable = false;
		}

		void HideNativeViewsFromAccessibility()
		{
			HideFromAccessibility(Handler?.ContainerView as Android.Views.View);
			HideFromAccessibility(Handler?.PlatformView as Android.Views.View);
		}
#endif
	}

	internal sealed partial class PopupPageLayout : Grid
	{
		readonly Popup popup;
		readonly Action tryExecuteTapOutsideOfPopupCommand;

		public PopupPageLayout(in Popup popupContent, in IPopupOptions options, in Action tryExecuteTapOutsideOfPopupCommand)
		{
			popup = popupContent;
			this.tryExecuteTapOutsideOfPopupCommand = tryExecuteTapOutsideOfPopupCommand;
			Background = BackgroundColor = null;

			PopupBorder = new Border
			{
				Content = popupContent
			};

			// Bind `Popup` values through to Border using OneWay Bindings
			// Never set values on the Popup here: a local value or a binding takes precedence over a Style, including an implicit Style applied once the PopupPage is displayed
			// While Popup.BackgroundColor is null, BackgroundColorConverter displays DefaultPopupSettings.BackgroundColor, or DefaultPopupSettings.BackgroundColorDark while the app uses the dark theme
			PopupBorder.SetBinding(Border.MarginProperty, static (Popup popup) => popup.Margin, source: popupContent, mode: BindingMode.OneWay, converter: new MarginConverter());
			PopupBorder.SetBinding(Border.BackgroundProperty, static (Popup popup) => popup.Background, source: popupContent, mode: BindingMode.OneWay);
			PopupBorder.SetBinding(Border.BackgroundColorProperty, new MultiBinding
			{
				Bindings =
				{
					BindingBase.Create(static (Popup popup) => popup.BackgroundColor, mode: BindingMode.OneWay, source: popupContent),
					new AppThemeColor
					{
						Light = Options.DefaultPopupSettings.BackgroundColor,
						Dark = Options.DefaultPopupSettings.BackgroundColorDark
					}.GetBinding()
				},
				Converter = new BackgroundColorConverter(),
				Mode = BindingMode.OneWay
			});
			PopupBorder.SetBinding(Border.VerticalOptionsProperty, static (Popup popup) => popup.VerticalOptions, source: popupContent, mode: BindingMode.OneWay, converter: new VerticalOptionsConverter());
			PopupBorder.SetBinding(Border.HorizontalOptionsProperty, static (Popup popup) => popup.HorizontalOptions, source: popupContent, mode: BindingMode.OneWay, converter: new HorizontalOptionsConverter());

			// Bind `PopupOptions` values through to Border using OneWay Bindings
			PopupBorder.SetBinding(Border.ShadowProperty, static (IPopupOptions options) => options.Shadow, source: options, mode: BindingMode.OneWay);
			PopupBorder.SetBinding(Border.StrokeProperty, static (IPopupOptions options) => options.Shape, source: options, mode: BindingMode.OneWay, converter: new BorderStrokeConverter());
			PopupBorder.SetBinding(Border.StrokeShapeProperty, static (IPopupOptions options) => options.Shape, source: options, mode: BindingMode.OneWay);
			PopupBorder.SetBinding(Border.StrokeThicknessProperty, static (IPopupOptions options) => options.Shape, source: options, mode: BindingMode.OneWay, converter: new BorderStrokeThicknessConverter());

			var overlayTapGestureRecognizer = new TapGestureRecognizer();
			overlayTapGestureRecognizer.Tapped += HandleOverlayTapped;
			TapGestureGestureOverlay = new PopupGestureOverlay();
			TapGestureGestureOverlay.GestureRecognizers.Add(overlayTapGestureRecognizer);

			Children.Add(TapGestureGestureOverlay);
			Children.Add(PopupBorder);

			AutomationProperties.SetIsInAccessibleTree(this, false);
			AutomationProperties.SetIsInAccessibleTree(PopupBorder, false);
		}

		public Border PopupBorder { get; }
		public PopupGestureOverlay TapGestureGestureOverlay { get; }

		protected override ILayoutManager CreateLayoutManager() => new PopupPageLayoutManager(this);

		void SetPopupMaximumDisplaySize(double widthConstraint, double heightConstraint)
		{
			var inset = Padding + PopupBorder.Margin + PopupBorder.Padding + PopupBorder.StrokeThickness;

			popup.SetMaximumDisplaySize(new Size(
				Math.Max(0, widthConstraint - inset.HorizontalThickness),
				Math.Max(0, heightConstraint - inset.VerticalThickness)));
		}

		void HandleOverlayTapped(object? sender, TappedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);

			var position = e.GetPosition(this);

			if (position is null)
			{
				return;
			}

			// Execute tapOutsideOfPopupCommand only if tap occurred outside the PopupBorder
			if (PopupBorder.Bounds.Contains(position.Value) is false)
			{
				tryExecuteTapOutsideOfPopupCommand();
			}
		}

		sealed class PopupPageLayoutManager(PopupPageLayout popupPageLayout) : GridLayoutManager(popupPageLayout)
		{
			public override Size Measure(double widthConstraint, double heightConstraint)
			{
				// The constraints are the space the layout arranges its children in (without the safe area it keeps free),
				// so a Popup larger than the window keeps Popup.Margin to its edges
				popupPageLayout.SetPopupMaximumDisplaySize(widthConstraint, heightConstraint);
				return base.Measure(widthConstraint, heightConstraint);
			}
		}

		sealed partial class BorderStrokeThicknessConverter : BaseConverterOneWay<Shape?, double>
		{
			public override double DefaultConvertReturnValue { get; set; } = Options.DefaultPopupOptionsSettings.Shape?.StrokeThickness ?? 0;

			public override double ConvertFrom(Shape? value, CultureInfo? culture) => value?.StrokeThickness ?? 0;
		}

		sealed partial class BorderStrokeConverter : BaseConverterOneWay<Shape?, Brush?>
		{
			public override Brush? DefaultConvertReturnValue { get; set; } = Options.DefaultPopupOptionsSettings.Shape?.Stroke;

			public override Brush? ConvertFrom(Shape? value, CultureInfo? culture) => value?.Stroke;
		}

		sealed partial class HorizontalOptionsConverter : BaseConverterOneWay<LayoutOptions, LayoutOptions>
		{
			public override LayoutOptions DefaultConvertReturnValue { get; set; } = Options.DefaultPopupSettings.HorizontalOptions;

			public override LayoutOptions ConvertFrom(LayoutOptions value, CultureInfo? culture) => value == LayoutOptions.Fill ? DefaultConvertReturnValue : value;
		}

		sealed partial class VerticalOptionsConverter : BaseConverterOneWay<LayoutOptions, LayoutOptions>
		{
			public override LayoutOptions DefaultConvertReturnValue { get; set; } = Options.DefaultPopupSettings.VerticalOptions;

			public override LayoutOptions ConvertFrom(LayoutOptions value, CultureInfo? culture) => value == LayoutOptions.Fill ? DefaultConvertReturnValue : value;
		}

		sealed class BackgroundColorConverter : IMultiValueConverter
		{
			public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => values switch
			{
				[Color popupBackgroundColor, ..] => popupBackgroundColor,
				[_, Color defaultBackgroundColor, ..] => defaultBackgroundColor,
				_ => Options.DefaultPopupSettings.BackgroundColor
			};

			public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
				throw new NotSupportedException($"{nameof(BackgroundColorConverter)} only supports one-way bindings.");
		}
	}

	sealed partial class PaddingConverter : BaseConverterOneWay<Thickness, Thickness>
	{
		public override Thickness DefaultConvertReturnValue { get; set; } = Options.DefaultPopupSettings.Padding;

		public override Thickness ConvertFrom(Thickness value, CultureInfo? culture) => value.IsEmpty || value.IsNaN ? DefaultConvertReturnValue : value;
	}

	sealed partial class MarginConverter : BaseConverterOneWay<Thickness, Thickness>
	{
		public override Thickness DefaultConvertReturnValue { get; set; } = Options.DefaultPopupSettings.Margin;

		public override Thickness ConvertFrom(Thickness value, CultureInfo? culture) => value.IsEmpty || value.IsNaN ? DefaultConvertReturnValue : value;
	}
}