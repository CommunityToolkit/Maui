using Android.Views;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// An activity that the system never started: it is not attached to anything, it reports the window that the test gives it
/// (<c>Activity.Window</c> is virtual) and, not being a FragmentActivity, it has no fragment manager. The toolkit's window code
/// accepts any activity, so this is how a test hands it a window that no running activity has, e.g. one whose decor view is not
/// attached to the window manager yet. Without a base context it cannot be used as a <see cref="Android.Content.Context"/> (resources,
/// creating views); with one it can, e.g. for toolkit code that creates a view with the current activity.
/// </summary>
public sealed class WindowOnlyActivity : Android.App.Activity
{
	readonly AWindow? window;

	/// <param name="window">The window that the activity reports.</param>
	/// <param name="baseContext">
	/// The context the activity wraps, or <see langword="null"/> for none. Never pass a context of the running app (the runner's activity, its
	/// base context, the application): Activity.attachBaseContext makes the activity the autofill client of the context it is given. Use a
	/// context of its own, see <see cref="CreateBaseContext"/>.
	/// </param>
	public WindowOnlyActivity(AWindow? window, Android.Content.Context? baseContext = null)
	{
		this.window = window;
		if (baseContext is not null)
		{
			AttachBaseContext(baseContext);
		}
	}

	public override AWindow? Window => window;

	/// <summary>A new context with the runner activity's configuration (a context of its own, no other object uses it) to wrap.</summary>
	public static Android.Content.Context CreateBaseContext()
	{
		var activity = AndroidSystemBars.Activity;
		var configuration = activity.Resources?.Configuration ?? throw new InvalidOperationException("The runner's activity has no configuration.");
		return activity.CreateConfigurationContext(configuration) ?? throw new InvalidOperationException("The runner's activity did not create a configuration context.");
	}
}

/// <summary>
/// A window that was never shown (the window of a <see cref="Android.App.Dialog"/> that nobody called <c>Show</c> on): its decor view
/// exists, but it is not attached to the window manager, so it has no layout parameters, the window reports no insets and the decor
/// view keeps what is posted to it until it is attached. <see cref="Activity"/> reports this window; use it with
/// <see cref="AndroidSystemBars.WithCurrentActivity"/>. It has a base context of its own, so toolkit code that creates a view with the current
/// activity while it is current works (and a test fails on what the code did with the window, not on a missing context). Create, use and
/// dispose it on the UI thread.
/// </summary>
public sealed class UnshownWindow : IDisposable
{
	readonly Android.App.Dialog dialog;

	public UnshownWindow()
	{
		dialog = new Android.App.Dialog(AndroidSystemBars.Activity);
		Window = dialog.Window ?? throw new InvalidOperationException("The dialog has no window.");
		Activity = new WindowOnlyActivity(Window, WindowOnlyActivity.CreateBaseContext());
	}

	public AWindow Window { get; }

	public WindowOnlyActivity Activity { get; }

	public ViewGroup DecorView => (ViewGroup)Window.DecorView;

	/// <summary>Shows the dialog: its decor view is attached to the window manager and runs what was posted to it.</summary>
	public void Show() => dialog.Show();

	public void Dispose()
	{
		if (dialog.IsShowing)
		{
			dialog.Dismiss();
		}

		Activity.Dispose();
		dialog.Dispose();
	}
}