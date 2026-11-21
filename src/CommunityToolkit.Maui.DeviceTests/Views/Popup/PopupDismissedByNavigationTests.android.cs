using Android.Widget;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Platform;
using Application = Microsoft.Maui.Controls.Application;
using Window = Microsoft.Maui.Controls.Window;

namespace CommunityToolkit.Maui.DeviceTests;

// The headless runner has no .NET MAUI Window and its activity cannot host a Window handler, so the tests run against a test-owned application whose Window
// has no handler (the way the Alerts tests do). .NET MAUI's own navigation runs, but a Window without a handler makes a Shell raise no NavigatedTo on the pages it
// pushes, so Opened is never raised here. Replacing the page of the Window is only covered on iOS, because .NET MAUI cannot do it without a Window handler.
public partial class PopupDismissedByNavigationTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public Task TwoPopupsOnTopOfAModalNavigationPageCloseOneAfterTheOther(bool closeWithClosePopupAsync)
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ =>
			PopupDismissedByNavigationScenarios.CloseTwoPopupsOneAfterTheOther(shell, closeWithClosePopupAsync, static (_, _) => Task.CompletedTask));
	}

	// A Window without a handler makes a Shell raise no NavigatedTo on the pages it pushes, so these two cover a PopupPage that navigation removes before NavigatedTo
	[Fact]
	public Task ShowPopupAsyncCompletesWhenShellPopsThePopupFromTheModalNavigationPage()
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ => PopupDismissedByNavigationScenarios.CompleteWhenShellPopsThePopupFromTheModalNavigationPage(shell, isOpenedRaised: false));
	}

	[Fact]
	public Task ShowPopupAsyncCompletesWhenShellSwitchesToAnotherShellElement()
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ => PopupDismissedByNavigationScenarios.CompleteWhenShellSwitchesToAnotherShellElement(shell, isOpenedRaised: false));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public Task CloseThatWaitedWhileAnotherPopupWasShownThrowsPopupBlockedException(bool onTopOfAModalNavigationPage)
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ =>
			PopupDismissedByNavigationScenarios.CloseThatWaitedWhileAnotherPopupWasShownThrowsPopupBlockedException(shell, onTopOfAModalNavigationPage));
	}

	[Fact]
	public Task ShowPopupAsyncCompletesWhenOtherCodePopsTheModalPage()
	{
		SetupBuilder();
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, async _ =>
		{
			var popup = new Popup();
			var closedCount = 0;
			var hasOpened = false;
			popup.Closed += (_, _) => closedCount++;
			popup.Opened += (_, _) => hasOpened = true;

			var showPopupTask = page.ShowPopupAsync(popup, new PopupOptions { Shape = null });
			await AssertionExtensions.AssertEventually(() => page.Navigation.ModalStack.OfType<PopupPage>().Count() == 1 && hasOpened, timeout: 5000, message: "The popup was never shown.");

			await page.Navigation.PopModalAsync(false);
			var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.True(result.WasDismissed);
			Assert.Equal(1, closedCount);
		});
	}

	// With hostShell, the Shell has a handler, because the fragments of its renderer look up their container in the activity's view tree: its platform view is attached to the activity first.
	Task RunInWindowAsync(Page root, ContentPage loadedPage, bool hostShell, Func<Window, Task> scenario) =>
		InvokeOnMainThreadAsync(async () =>
		{
			var previousApplication = Application.Current;
			FrameLayout? host = null;
			try
			{
				var application = new TestApplication(root);
				var window = (Window)((IApplication)application).CreateWindow(null);
				application.Handler = new StubApplicationHandler(MauiContext);

				if (hostShell)
				{
					root.ToHandler(MauiContext);
					host = new FrameLayout(AlertsAndroid.Activity);
					AlertsAndroid.Content.AddView(host);
					host.AddView((Android.Views.View)(root.Handler?.PlatformView ?? throw new InvalidOperationException("The Shell has no platform view.")));
					await loadedPage.OnLoadedAsync();
				}

				await scenario(window);
			}
			finally
			{
				if (host is not null)
				{
					host.RemoveAllViews();
					AlertsAndroid.Content.RemoveView(host);
				}

				root.Handler?.DisconnectHandler();
				Application.Current = previousApplication;
			}
		});

	sealed class TestApplication(Page mainPage) : Application
	{
		protected override Window CreateWindow(IActivationState? activationState) => new(mainPage);
	}
}