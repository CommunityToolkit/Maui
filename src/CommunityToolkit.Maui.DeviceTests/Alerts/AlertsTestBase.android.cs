using CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;
using Microsoft.Maui.ApplicationModel;
using Xunit.Sdk;
using Application = Microsoft.Maui.Controls.Application;
using Window = Microsoft.Maui.Controls.Window;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class AlertsTestBase
{
	Application? previousApplication;
	ToastObserver? toastObserver;
	bool releaseAccessibilityService;

	protected ToastObserver ToastObserver => toastObserver ?? throw new InvalidOperationException($"{nameof(PreparePlatformAsync)} was not called, or the test class does not observe toasts.");

	// Only the Toast tests need the observer of the system's toast events: the Snackbar tests read the views of the activity.
	protected virtual bool ObservesToasts => false;

	// The headless runner's application has no MAUI Window, and Snackbar reads Application.Current.Windows[0] and the application
	// handler's services (IFontManager). So every test runs against a test-owned application with one window, whose handler
	// carries this test's services. CleanUpAlertsAsync puts the runner's application back.
	protected async Task PreparePlatformAsync()
	{
		// The toasts are observed through the instrumentation of the headless runner (XHarness, CI). The visual runner, which runs the
		// RequiresUserInteraction tests by hand, has no instrumentation.
		Skip.If(ObservesToasts && MauiTestInstrumentation.Current is null, "The Toast tests observe toasts through the UiAutomation of the headless runner's instrumentation, which the visual runner does not have.");

		await InvokeOnMainThreadAsync(() => InstallTestApplication(new ContentPage()));

		if (ObservesToasts)
		{
			// UiAutomation connects on its own thread and delivers the accessibility events (the text of every toast) to the observer.
			// Connecting it switches an accessibility service on for the whole process; CleanUpAlertsAsync switches it off again, so that
			// the tests that run later, of any area of the app, see no accessibility service (see ToastObserver). Only a service that this
			// test switches on is switched off: one that already runs (a real accessibility service of the device) stays as it is.
			var accessibilityManager = GetAccessibilityManager();
			releaseAccessibilityService = !accessibilityManager.IsEnabled;
			toastObserver = await Task.Run(() => ToastObserver.Create(MauiTestInstrumentation.Current.UiAutomation ?? throw new InvalidOperationException("The instrumentation has no UiAutomation.")));

			// The system reports a toast through an accessibility event only once it knows that an accessibility service runs.
			await AssertHelpers.AssertEventually(() => accessibilityManager.IsEnabled, timeout: 5000, message: "The UiAutomation accessibility service did not connect, so the system would not report the toasts.");
		}
	}

	static Android.Views.Accessibility.AccessibilityManager GetAccessibilityManager() =>
		Android.App.Application.Context.GetSystemService(Android.Content.Context.AccessibilityService) as Android.Views.Accessibility.AccessibilityManager
			?? throw new InvalidOperationException("The app has no AccessibilityManager.");

	protected TestAlertsApplication InstallTestApplication(Page? mainPage, bool createWindow = true, bool installHandler = true)
	{
		previousApplication ??= Application.Current;

		var application = new TestAlertsApplication(mainPage);
		if (createWindow)
		{
			((IApplication)application).CreateWindow(null);
		}

		if (installHandler)
		{
			application.Handler = new StubApplicationHandler(MauiContext);
		}

		return application;
	}

	// Application.Current can also be set to null (or to the runner's application) by a test: remember what to restore.
	protected void RememberApplicationToRestore() => previousApplication ??= Application.Current;

	// Ends every test with nothing on screen. An alert that cannot be taken away (a test disposed its Java peer while it was shown, so
	// neither Dismiss nor Cancel reaches it any more) would leak into the next test: a stray Snackbar.Dismissed event, or a toast that
	// the system displays before the next test's toast. Such an alert is removed or waited out, and the test that left it fails.
	async Task CleanUpAlertsAsync()
	{
		var leftovers = new List<string>();
		var accessibilityServiceLeftOn = false;
		try
		{
			// Dismiss through the native objects, so that cleaning up never depends on the toolkit code under test or on the installed application.
			if (GetPlatformSnackbar() is Google.Android.Material.Snackbar.Snackbar snackbar && snackbar.Handle != IntPtr.Zero)
			{
				snackbar.Dismiss();
			}

			if (GetPlatformToast() is Android.Widget.Toast toast && toast.Handle != IntPtr.Zero)
			{
				toast.Cancel();
			}

			if (await AlertsAndroid.RemoveSnackbarsThatCannotBeDismissedAsync())
			{
				leftovers.Add("a snackbar that could not be dismissed (its Java peer was disposed while it was shown) was still on screen after 10 s");
			}

			if (toastObserver is not null && await toastObserver.WaitForToastsThatCannotBeCancelledAsync())
			{
				leftovers.Add("a toast whose Java peer was disposed while it was displayed could not be cancelled, so the clean-up waited until the system hid it");
			}
		}
		finally
		{
			toastObserver?.Dispose();
			toastObserver = null;
			ResetToolkitAlertState();

			if (previousApplication is not null)
			{
				Application.Current = previousApplication;
				previousApplication = null;
			}

			if (releaseAccessibilityService)
			{
				releaseAccessibilityService = false;
				accessibilityServiceLeftOn = !await ToastObserver.ReleaseAccessibilityServiceAsync(GetAccessibilityManager());
			}
		}

		if (leftovers.Count > 0)
		{
			throw new XunitException($"The test left alerts behind that the next test would have seen: {string.Join("; ", leftovers)}. Take an alert off screen before disposing it.");
		}

		if (accessibilityServiceLeftOn)
		{
			throw new XunitException("The accessibility service that the toast observer switched on (the instrumentation's UiAutomation) still ran 5 s after it was released, so every later test of the app would have run with accessibility enabled.");
		}
	}

	protected sealed class TestAlertsApplication(Page? mainPage) : Application
	{
		protected override Window CreateWindow(IActivationState? activationState) => mainPage is null ? new Window() : new Window(mainPage);
	}
}