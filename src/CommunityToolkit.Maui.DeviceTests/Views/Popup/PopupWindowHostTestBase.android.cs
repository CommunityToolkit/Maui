using Android.Widget;
using Microsoft.Maui.Platform;
using Application = Microsoft.Maui.Controls.Application;
using Window = Microsoft.Maui.Controls.Window;

namespace CommunityToolkit.Maui.DeviceTests;

// The headless runner has no .NET MAUI Window and its activity cannot host a Window handler, so the tests run against a test-owned application whose Window
// has no handler (the way the Alerts tests do). .NET MAUI's own navigation runs, but a Window without a handler makes a Shell raise no NavigatedTo on the pages it
// pushes, so Opened is never raised here. Replacing the page of the Window is only covered on iOS, Mac Catalyst and Windows, because .NET MAUI cannot do it without a Window handler.
public abstract partial class PopupWindowHostTestBase
{
	// With hostShell, the Shell has a handler, because the fragments of its renderer look up their container in the activity's view tree: its platform view is attached to the activity first.
	protected Task RunInWindowAsync(Page root, ContentPage loadedPage, bool hostShell, Func<Window, Task> scenario) =>
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

	// Android has no native state to compare: the popup is a .NET MAUI modal page there, which the scenarios already assert through the modal stack
	protected static Task NoNativeStateToAssertAsync(bool isPresented) => Task.CompletedTask;

	sealed class TestApplication(Page mainPage) : Application
	{
		protected override Window CreateWindow(IActivationState? activationState) => new(mainPage);
	}
}