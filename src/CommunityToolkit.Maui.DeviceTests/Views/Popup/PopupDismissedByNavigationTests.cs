using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// The tests in this file are controls that raise Window.ModalPopped, which .NET MAUI raises when navigation removes a modal page, on a Window that has no
// modal stack. The platform files of this class show the PopupPage in a Window of its own and navigate with .NET MAUI's own navigation.
[Category(TestCategory.Popup)]
[Collection(TestCollections.DisableParallelization)]
public partial class PopupDismissedByNavigationTests : MauiWindowHostTestBase
{
	[Fact]
	public Task PopupClosedIsRaisedOnceWhenNavigationRemovesThePopupPageWithoutCloseAsync()
	{
		SetupBuilder();

		return InvokeOnMainThreadAsync(async () =>
		{
			var popup = new Popup();
			var popupClosedCount = 0;
			var popupPageClosedCount = 0;
			popup.Closed += (_, _) => popupClosedCount++;
			var popupPage = new NavigablePopupPage(popup);
			popupPage.PopupClosed += (_, result) =>
			{
				popupPageClosedCount++;
				Assert.True(result.WasDismissedByTappingOutsideOfPopup);
			};
			var window = new Window(popupPage);

			popupPage.RaiseNavigatedTo();
			RaiseModalPopped(window, popupPage);
			RaiseModalPopped(window, popupPage);

			// The result is raised through the Dispatcher, after .NET MAUI has finished the navigation that removed the page
			await AssertionExtensions.AssertEventually(() => popupClosedCount == 1, timeout: 5000, message: "Popup.Closed was not raised.");
			// There is no event to wait for when nothing more happens: give a second report, dispatched by the second ModalPopped, time to arrive
			await Task.Delay(100);

			Assert.Equal(1, popupClosedCount);
			Assert.Equal(1, popupPageClosedCount);
		});
	}

	[Fact]
	public Task PopupClosedIsNotRaisedWhenNavigationRemovesAnotherModalPage()
	{
		SetupBuilder();

		return InvokeOnMainThreadAsync(async () =>
		{
			var popupPageClosedCount = 0;
			var popupPage = new NavigablePopupPage(new Popup());
			popupPage.PopupClosed += (_, _) => popupPageClosedCount++;
			var window = new Window(popupPage);

			popupPage.RaiseNavigatedTo();
			RaiseModalPopped(window, new ContentPage());

			// A wrong report is raised through the Dispatcher, so it arrives after the call that caused it: give it time to arrive, as the positive test does
			await Task.Delay(100);

			Assert.Equal(0, popupPageClosedCount);
		});
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			ReleaseNavigationSemaphoreHeldByCloseAsyncThatNeverCompleted();
		}
	}

	// A CloseAsync that never completes keeps the static semaphore of PopupPage, so every popup test that ran after a failed one would fail in its own arrange step
	static void ReleaseNavigationSemaphoreHeldByCloseAsyncThatNeverCompleted()
	{
		var navigationSemaphore = (SemaphoreSlim?)typeof(PopupPage).GetField("navigationSemaphoreSlim", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
		if (navigationSemaphore is not null && navigationSemaphore.CurrentCount == 0)
		{
			navigationSemaphore.Release();
		}
	}

	// Window.OnModalPopped is internal to .NET MAUI
	static void RaiseModalPopped(Window window, Page modalPage)
	{
		var onModalPopped = typeof(Window).GetMethod("OnModalPopped", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("Window.OnModalPopped was not found");
		onModalPopped.Invoke(window, [modalPage]);
	}

	sealed class NavigablePopupPage(Popup popup) : PopupPage(popup, new PopupOptions { Shape = null })
	{
		// The constructors of the event arguments are internal to .NET MAUI, and PopupPage does not read them
		public void RaiseNavigatedTo() => OnNavigatedTo((NavigatedToEventArgs)RuntimeHelpers.GetUninitializedObject(typeof(NavigatedToEventArgs)));
	}
}
