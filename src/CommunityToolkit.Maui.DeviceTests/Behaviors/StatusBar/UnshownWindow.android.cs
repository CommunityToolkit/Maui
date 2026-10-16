using Android.Views;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// An activity that the system never started and that, not being a FragmentActivity, has no fragment manager. The toolkit's window code
/// accepts any activity, so this is how a test hands it a window that no running activity has, e.g. one whose decor view is not attached
/// to the window manager yet. Without a base context it cannot be used as a <see cref="Android.Content.Context"/> (resources, creating views).
/// </summary>
public sealed class WindowOnlyActivity : Android.App.Activity
{
	readonly AWindow? window;

	/// <param name="baseContext">
	/// Never pass a context of the running app (the runner's activity, its base context, the application): Activity.attachBaseContext makes
	/// the activity the autofill client of the context it is given. Use a context of its own, see <see cref="CreateBaseContext"/>.
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

	// A new context of its own that no other object uses, see the baseContext parameter of the constructor.
	public static Android.Content.Context CreateBaseContext()
	{
		var activity = AndroidSystemBars.Activity;
		var configuration = activity.Resources?.Configuration ?? throw new InvalidOperationException("The runner's activity has no configuration.");
		return activity.CreateConfigurationContext(configuration) ?? throw new InvalidOperationException("The runner's activity did not create a configuration context.");
	}
}

/// <summary>
/// A window that was never shown: its decor view exists, but it is not attached to the window manager, so it has no layout parameters, the
/// window reports no insets and the decor view keeps what is posted to it until it is attached. <see cref="Activity"/> has a base context of
/// its own, so toolkit code that creates a view with the current activity while it is current (<see cref="AndroidSystemBars.WithCurrentActivity"/>)
/// works, and a test fails on what the code did with the window, not on a missing context. Create, use and dispose it on the UI thread.
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