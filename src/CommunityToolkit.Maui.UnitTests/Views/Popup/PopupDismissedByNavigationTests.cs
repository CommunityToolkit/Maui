using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Xunit;
using Application = Microsoft.Maui.Controls.Application;
using NavigationPage = Microsoft.Maui.Controls.NavigationPage;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupDismissedByNavigationTests : BaseViewTest
{
	public enum NavigationPageRemoval
	{
		PopAsync,
		RemovePage,
		PopToRootAsync
	}

	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData(RootPageType.Shell)]
	[InlineData(RootPageType.ContentPage)]
	[InlineData(RootPageType.NavigationPage)]
	public async Task ShowPopupAsync_ShouldCompleteAsDismissedAndRaiseClosedOnce_WhenModalIsPoppedWithoutCloseAsync(RootPageType rootPageType)
	{
		var navigation = SetRootPage(rootPageType);
		var popup = new Popup();
		var closedCount = 0;
		popup.Closed += (_, _) => closedCount++;

		var showPopupTask = navigation.ShowPopupAsync(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = Assert.IsType<PopupPage>(Assert.Single(navigation.ModalStack));

		await navigation.PopModalAsync(false);
		var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(1, closedCount);

		// CloseAsync of a PopupPage that navigation already removed must not report the result a second time
		await Assert.ThrowsAnyAsync<InvalidPopupOperationException>(() => popupPage.CloseAsync(new PopupResult(false), TestContext.Current.CancellationToken));
		Assert.Equal(1, closedCount);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldCompleteAsDismissed_WhenShellSwitchesToAnotherShellElement()
	{
		var shell = new Shell();
		shell.Items.Add(new ShellContent { Route = "first", Content = new ContentPage() });
		shell.Items.Add(new ShellContent { Route = "second", Content = new ContentPage() });
		var window = Application.Current?.Windows[0] ?? throw new InvalidOperationException("Unable to locate Window");
		window.Page = shell;
		var popup = new Popup();
		var closedCount = 0;
		popup.Closed += (_, _) => closedCount++;

		var showPopupTask = shell.ShowPopupAsync(popup, PopupOptions.Empty, token: TestContext.Current.CancellationToken);
		for (var i = 0; i < 100 && shell.Navigation.ModalStack.Count == 0; i++)
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
		}

		Assert.Single(shell.Navigation.ModalStack);

		await shell.GoToAsync("//second");
		var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(shell.Navigation.ModalStack);
		Assert.Equal(1, closedCount);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldCompleteBothPopupsAsDismissed_WhenTheModalStackIsPoppedWithoutCloseAsync()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);
		var closedCounts = new int[2];
		var lowerPopup = new Popup();
		var upperPopup = new Popup();
		lowerPopup.Closed += (_, _) => closedCounts[0]++;
		upperPopup.Closed += (_, _) => closedCounts[1]++;

		var lowerTask = navigation.ShowPopupAsync(lowerPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		var upperTask = navigation.ShowPopupAsync(upperPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		Assert.Equal(2, navigation.ModalStack.Count);

		await navigation.PopModalAsync(false);
		await navigation.PopModalAsync(false);
		await Task.WhenAll(lowerTask, upperTask).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.Equal([1, 1], closedCounts);
	}

	[Fact]
	public async Task ShowPopupAsync_ShouldCompleteAsDismissed_WhenModalNavigationPageContainingThePopupIsPopped()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);
		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var closedCount = 0;
		popupPage.PopupClosed += (_, _) => closedCount++;
		var modalNavigationPage = new NavigationPage(popupPage);

		await navigation.PushModalAsync(modalNavigationPage, false);
		await navigation.PopModalAsync(false);

		Assert.Equal(1, closedCount);
	}

	[Fact]
	public async Task PopupClosed_ShouldNotBeRaisedByTheModalPopOfAnotherPage()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);
		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var closedCount = 0;
		popupPage.PopupClosed += (_, _) => closedCount++;

		await navigation.PushModalAsync(popupPage, false);
		await navigation.PushModalAsync(new ContentPage(), false);
		await navigation.PopModalAsync(false);

		Assert.Equal(0, closedCount);
		Assert.Same(popupPage, Assert.Single(navigation.ModalStack));
	}

	// The Android QA walk found two popups, shown with IPopupService.ShowPopupAsync<T>(Shell.Current) on top of a modal NavigationPage, that could not be closed.
	// With a Shell as the root page, .NET MAUI pushes a page that is shown modally on top of a modal NavigationPage onto the navigation stack of that NavigationPage.
	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(RootPageType.Shell)]
	[InlineData(RootPageType.ContentPage)]
	public async Task CloseAsync_ShouldCloseTwoPopupsOneAfterTheOther_WhenTheyAreShownOnTopOfAModalNavigationPage(RootPageType rootPageType)
	{
		var shown = await ShowTwoPopupsOnTopOfAModalNavigationPageAsync(rootPageType);
		var (navigation, modalNavigationPage, lowerPopup, upperPopup, lowerTask, upperTask) = shown;

		await upperPopup.CloseAsync(TestContext.Current.CancellationToken);
		await upperTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		Assert.Equal(1, CountPopupPages(navigation, modalNavigationPage));

		await lowerPopup.CloseAsync(TestContext.Current.CancellationToken);
		await lowerTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.Equal(0, CountPopupPages(navigation, modalNavigationPage));
		Assert.Same(modalNavigationPage, Assert.Single(navigation.ModalStack));
	}

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(RootPageType.Shell)]
	[InlineData(RootPageType.ContentPage)]
	public async Task ClosePopupAsync_ShouldCloseTwoPopupsOneAfterTheOther_WhenTheyAreShownOnTopOfAModalNavigationPage(RootPageType rootPageType)
	{
		var (navigation, modalNavigationPage, _, _, lowerTask, upperTask) = await ShowTwoPopupsOnTopOfAModalNavigationPageAsync(rootPageType);

		await navigation.ClosePopupAsync(TestContext.Current.CancellationToken);
		await upperTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		await navigation.ClosePopupAsync(TestContext.Current.CancellationToken);
		await lowerTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.Equal(0, CountPopupPages(navigation, modalNavigationPage));
		Assert.Same(modalNavigationPage, Assert.Single(navigation.ModalStack));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncOfValueType_ShouldCompleteAsDismissedAndThrowOnResult_WhenModalIsPoppedWithoutCloseAsync()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);

		var showPopupTask = navigation.ShowPopupAsync<int>(new Popup<int>(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		await navigation.PopModalAsync(false);
		var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Throws<PopupResultException>(() => result.Result);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncOfReferenceType_ShouldCompleteAsDismissedWithNullResult_WhenModalIsPoppedWithoutCloseAsync()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);

		var showPopupTask = navigation.ShowPopupAsync<string>(new Popup<string>(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		await navigation.PopModalAsync(false);
		var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Null(result.Result);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task ShowPopupAsync_ShouldCompleteAsDismissedAndRaiseClosedOnce_WhenShellPopsThePopupFromTheModalNavigationPage()
	{
		var (navigation, modalNavigationPage, _, task, closedCount) = await ShowOnePopupOnTopOfAModalNavigationPageAsync(RootPageType.Shell);
		var shell = Assert.IsType<Shell>(Application.Current?.Windows[0].Page);

		await shell.GoToAsync("..");
		var result = await task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(1, closedCount());
		Assert.Empty(navigation.ModalStack.OfType<PopupPage>());

		// GoToAsync("..") pops one page: the PopupPage leaves the modal NavigationPage, and the NavigationPage stays on the modal stack
		Assert.Same(modalNavigationPage, Assert.Single(navigation.ModalStack));
		Assert.Single(modalNavigationPage.Navigation.NavigationStack);
	}

	// PopAsync, RemovePage and PopToRootAsync each remove the PopupPage from the NavigationPage without raising Window.ModalPopped
	[Theory]
	[InlineData(NavigationPageRemoval.PopAsync)]
	[InlineData(NavigationPageRemoval.RemovePage)]
	[InlineData(NavigationPageRemoval.PopToRootAsync)]
	public async Task PopupClosed_ShouldBeRaisedOnce_WhenPopupIsRemovedFromAModalNavigationPageByOtherCode(NavigationPageRemoval removal)
	{
		var navigation = SetRootPage(RootPageType.ContentPage);
		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var closedCount = 0;
		popupPage.PopupClosed += (_, _) => closedCount++;
		var modalNavigationPage = new NavigationPage(new ContentPage());
		await navigation.PushModalAsync(modalNavigationPage, false);
		await modalNavigationPage.Navigation.PushAsync(popupPage, false);
		RaiseNavigatedTo(popupPage);

		await RemoveFromNavigationPageAsync(modalNavigationPage, popupPage, removal);

		Assert.Equal(1, closedCount);
	}

	// .NET MAUI raises NavigatedTo after the push completes, and not at all in a Window without a handler, so a removal can come first
	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task PopupClosed_ShouldBeRaisedOnce_WhenNavigationRemovesThePopupPageBeforeNavigatedTo()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);
		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var closedCount = 0;
		popupPage.PopupClosed += (_, _) => closedCount++;
		var modalNavigationPage = new NavigationPage(new ContentPage());
		await navigation.PushModalAsync(modalNavigationPage, false);

		await popupPage.ShowAsync(new PushesIntoNavigationPage(modalNavigationPage), TestContext.Current.CancellationToken);
		Assert.Equal(0, closedCount);
		await modalNavigationPage.Navigation.PopAsync(false);

		Assert.Equal(1, closedCount);
	}

	// MockDispatcher runs an action synchronously, which would hide whether the result is raised from inside the navigation that removed the PopupPage
	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldRaiseClosedAfterTheNavigationThatRemovedThePopupPageHasFinished()
	{
		var dispatchedActions = new Queue<Action>();
		DispatcherProvider.SetCurrent(new QueueingDispatcherProvider(dispatchedActions));
		var navigation = SetRootPage(RootPageType.ContentPage);
		var popup = new Popup();
		var closedCount = 0;
		popup.Closed += (_, _) => closedCount++;
		var showPopupTask = navigation.ShowPopupAsync(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		await navigation.PopModalAsync(false);

		Assert.Equal(0, closedCount);
		Assert.False(showPopupTask.IsCompleted);

		while (dispatchedActions.TryDequeue(out var action))
		{
			action();
		}

		var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(1, closedCount);
	}

	// A CloseAsync that waited while navigation removed its PopupPage must not pop the page that is on top of the navigation stack by then
	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task CloseAsync_ShouldThrowPopupNotFoundException_WhenNavigationRemovesThePopupPageWhileItWaitsForOtherNavigation()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);
		var popup = new Popup();
		var closedCount = 0;
		popup.Closed += (_, _) => closedCount++;
		var showPopupTask = navigation.ShowPopupAsync(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = Assert.IsType<PopupPage>(Assert.Single(navigation.ModalStack));
		var navigationSemaphore = GetNavigationSemaphore();
		var otherPage = new ContentPage();

		await navigationSemaphore.WaitAsync(TestContext.Current.CancellationToken);
		Task closeTask;
		try
		{
			closeTask = popupPage.CloseAsync(new PopupResult(false), CancellationToken.None);
			await navigation.PopModalAsync(false);
			await navigation.PushModalAsync(otherPage, false);
		}
		finally
		{
			navigationSemaphore.Release();
		}

		await Assert.ThrowsAsync<PopupNotFoundException>(() => closeTask);
		var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(1, closedCount);
		Assert.Same(otherPage, Assert.Single(navigation.ModalStack));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task CloseAsync_ShouldRaiseClosedOnce_WhenThePopupIsInAModalNavigationPage()
	{
		var (_, _, popup, task, closedCount) = await ShowOnePopupOnTopOfAModalNavigationPageAsync(RootPageType.Shell);

		await popup.CloseAsync(TestContext.Current.CancellationToken);
		var result = await task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(1, closedCount());
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldCompleteAsDismissedAndRaiseClosedOnce_WhenThePageOfTheWindowIsReplaced()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);
		var popup = new Popup();
		var closedCount = 0;
		popup.Closed += (_, _) => closedCount++;
		var showPopupTask = navigation.ShowPopupAsync(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		var window = Application.Current?.Windows[0] ?? throw new InvalidOperationException("Unable to locate Window");

		window.Page = new ContentPage();
		var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(1, closedCount);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldReleasePopupPageAndPopup_WhenModalIsPoppedWithoutCloseAsync()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);

		var (showPopupTask, popupReference, popupPageReference) = await ShowAndDismissWithoutKeepingReferencesAsync(navigation);
		await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		for (var i = 0; i < 10 && (popupReference.IsAlive || popupPageReference.IsAlive); i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await Task.Delay(10, TestContext.Current.CancellationToken);
		}

		Assert.False(popupPageReference.IsAlive);
		Assert.False(popupReference.IsAlive);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task CloseAsync_ShouldNotLoseTheDismissal_WhenNavigationRemovesThePopupPageWhileItWaitsForOtherNavigation()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);
		var popup = new Popup();
		var closedCount = 0;
		popup.Closed += (_, _) => closedCount++;
		var showPopupTask = navigation.ShowPopupAsync(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = Assert.IsType<PopupPage>(Assert.Single(navigation.ModalStack));
		var navigationSemaphore = GetNavigationSemaphore();
		using var closeCancellationTokenSource = new CancellationTokenSource();

		await navigationSemaphore.WaitAsync(TestContext.Current.CancellationToken);
		Task closeTask;
		try
		{
			closeTask = popupPage.CloseAsync(new PopupResult(false), closeCancellationTokenSource.Token);
			await navigation.PopModalAsync(false);
			closeCancellationTokenSource.Cancel();

			// SemaphoreSlim observes the cancellation of a pending WaitAsync asynchronously; releasing before it does would hand the semaphore to the canceled CloseAsync
			await Task.WhenAny(closeTask, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
		}
		finally
		{
			navigationSemaphore.Release();
		}

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => closeTask);
		var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(1, closedCount);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldLetAClosedHandlerShowAnotherPopup_WhenShellSwitchesToAnotherShellElement()
	{
		var shell = new Shell();
		shell.Items.Add(new ShellContent { Route = "first", Content = new ContentPage() });
		shell.Items.Add(new ShellContent { Route = "second", Content = new ContentPage() });
		var window = Application.Current?.Windows[0] ?? throw new InvalidOperationException("Unable to locate Window");
		window.Page = shell;
		var popup = new Popup();
		var secondPopup = new Popup();
		Task<IPopupResult>? secondShowPopupTask = null;
		popup.Closed += (_, _) => secondShowPopupTask = shell.ShowPopupAsync(secondPopup, PopupOptions.Empty, token: TestContext.Current.CancellationToken);

		var showPopupTask = shell.ShowPopupAsync(popup, PopupOptions.Empty, token: TestContext.Current.CancellationToken);
		for (var i = 0; i < 100 && shell.Navigation.ModalStack.Count == 0; i++)
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
		}

		await shell.GoToAsync("//second");
		await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		for (var i = 0; i < 200 && shell.Navigation.ModalStack.OfType<PopupPage>().Count() != 1; i++)
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
		}

		Assert.NotNull(secondShowPopupTask);
		Assert.Single(shell.Navigation.ModalStack.OfType<PopupPage>());
	}

	static SemaphoreSlim GetNavigationSemaphore() =>
		(SemaphoreSlim?)typeof(PopupPage).GetField("navigationSemaphoreSlim", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null)
		?? throw new InvalidOperationException("PopupPage.navigationSemaphoreSlim was not found");

	static Task RemoveFromNavigationPageAsync(NavigationPage navigationPage, PopupPage popupPage, NavigationPageRemoval removal)
	{
		switch (removal)
		{
			case NavigationPageRemoval.PopAsync:
				return navigationPage.Navigation.PopAsync(false);
			case NavigationPageRemoval.RemovePage:
				navigationPage.Navigation.RemovePage(popupPage);
				return Task.CompletedTask;
			case NavigationPageRemoval.PopToRootAsync:
				return navigationPage.Navigation.PopToRootAsync(false);
			default:
				throw new NotSupportedException($"{removal} is not supported");
		}
	}

	// The unit test environment has no NavigationPage handler, so PushAsync does not raise NavigatedTo, which starts the observation of the PopupPage.
	// The constructors of the event arguments are internal to .NET MAUI, and PopupPage does not read them
	static void RaiseNavigatedTo(PopupPage popupPage)
	{
		var onNavigatedTo = typeof(Page).GetMethod("OnNavigatedTo", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("Page.OnNavigatedTo was not found");
		onNavigatedTo.Invoke(popupPage, [RuntimeHelpers.GetUninitializedObject(typeof(NavigatedToEventArgs))]);
	}

	// Not inlined, so that no local of the test keeps the PopupPage or the Popup alive
	[MethodImpl(MethodImplOptions.NoInlining)]
	static async Task<(Task<IPopupResult> ShowPopupTask, WeakReference PopupReference, WeakReference PopupPageReference)> ShowAndDismissWithoutKeepingReferencesAsync(INavigation navigation)
	{
		var popup = new Popup();
		var showPopupTask = navigation.ShowPopupAsync(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPageReference = new WeakReference(Assert.Single(navigation.ModalStack));

		await navigation.PopModalAsync(false);
		return (showPopupTask, new WeakReference(popup), popupPageReference);
	}

	static async Task<(INavigation Navigation, NavigationPage ModalNavigationPage, Popup Popup, Task<IPopupResult> ShowPopupTask, Func<int> ClosedCount)> ShowOnePopupOnTopOfAModalNavigationPageAsync(RootPageType rootPageType)
	{
		var navigation = SetRootPage(rootPageType);
		var modalNavigationPage = new NavigationPage(new ContentPage());
		await navigation.PushModalAsync(modalNavigationPage, false);

		var popup = new Popup();
		var closedCount = 0;
		popup.Closed += (_, _) => closedCount++;
		var showPopupTask = navigation.ShowPopupAsync(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		for (var i = 0; i < 200 && CountPopupPages(navigation, modalNavigationPage) < 1; i++)
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
		}

		Assert.Equal(1, CountPopupPages(navigation, modalNavigationPage));
		return (navigation, modalNavigationPage, popup, showPopupTask, () => closedCount);
	}

	static async Task<(INavigation Navigation, NavigationPage ModalNavigationPage, Popup LowerPopup, Popup UpperPopup, Task<IPopupResult> LowerTask, Task<IPopupResult> UpperTask)> ShowTwoPopupsOnTopOfAModalNavigationPageAsync(RootPageType rootPageType)
	{
		var navigation = SetRootPage(rootPageType);
		var modalNavigationPage = new NavigationPage(new ContentPage());
		await navigation.PushModalAsync(modalNavigationPage, false);

		var lowerPopup = new Popup();
		var upperPopup = new Popup();
		var lowerTask = navigation.ShowPopupAsync(lowerPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		var upperTask = navigation.ShowPopupAsync(upperPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		for (var i = 0; i < 200 && CountPopupPages(navigation, modalNavigationPage) < 2; i++)
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
		}

		Assert.Equal(2, CountPopupPages(navigation, modalNavigationPage));
		return (navigation, modalNavigationPage, lowerPopup, upperPopup, lowerTask, upperTask);
	}

	static int CountPopupPages(INavigation navigation, NavigationPage modalNavigationPage) =>
		navigation.ModalStack.OfType<PopupPage>().Count() + modalNavigationPage.Navigation.NavigationStack.OfType<PopupPage>().Count();

	sealed class QueueingDispatcherProvider(Queue<Action> dispatchedActions) : IDispatcherProvider
	{
		readonly QueueingDispatcher dispatcher = new(dispatchedActions);

		public IDispatcher? GetForCurrentThread() => dispatcher;
	}

	sealed class QueueingDispatcher(Queue<Action> dispatchedActions) : IDispatcher
	{
		public bool IsDispatchRequired => true;

		public IDispatcherTimer CreateTimer() => throw new NotSupportedException();

		public bool Dispatch(Action action)
		{
			dispatchedActions.Enqueue(action);
			return true;
		}

		public bool DispatchDelayed(TimeSpan delay, Action action) => Dispatch(action);
	}

	// Pushes the PopupPage into the modal NavigationPage the way .NET MAUI does for a popup shown on top of one, which raises no NavigatedTo in the unit test environment
	sealed class PushesIntoNavigationPage(NavigationPage navigationPage) : INavigation
	{
		readonly INavigation navigation = navigationPage.Navigation;

		public IReadOnlyList<Page> NavigationStack => navigation.NavigationStack;

		public IReadOnlyList<Page> ModalStack => navigation.ModalStack;

		public void InsertPageBefore(Page page, Page before) => navigation.InsertPageBefore(page, before);

		public Task<Page> PopAsync() => navigation.PopAsync();

		public Task<Page> PopAsync(bool animated) => navigation.PopAsync(animated);

		public Task<Page> PopModalAsync() => navigation.PopModalAsync();

		public Task<Page> PopModalAsync(bool animated) => navigation.PopModalAsync(animated);

		public Task PopToRootAsync() => navigation.PopToRootAsync();

		public Task PopToRootAsync(bool animated) => navigation.PopToRootAsync(animated);

		public Task PushAsync(Page page) => navigation.PushAsync(page);

		public Task PushAsync(Page page, bool animated) => navigation.PushAsync(page, animated);

		public Task PushModalAsync(Page page) => navigation.PushAsync(page);

		public Task PushModalAsync(Page page, bool animated) => navigation.PushAsync(page, animated);

		public void RemovePage(Page page) => navigation.RemovePage(page);
	}
}
