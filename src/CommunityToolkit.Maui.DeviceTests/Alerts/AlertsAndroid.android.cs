using System.Reflection;
using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.ApplicationModel;
using MaterialSnackbar = Google.Android.Material.Snackbar.Snackbar;
using View = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// Reads the Android views that Snackbar creates (a Material SnackbarLayout under the activity's content view).
static class AlertsAndroid
{
	public static Activity Activity => Platform.CurrentActivity ?? throw new InvalidOperationException("The test app has no current activity.");

	public static ViewGroup Content => Activity.Window!.DecorView.FindViewById<ViewGroup>(Android.Resource.Id.Content)!;

	public static float Density => Activity.Resources!.DisplayMetrics!.Density;

	// Every Snackbar layout in the activity's view tree, including one that is animating in or out.
	public static List<MaterialSnackbar.SnackbarLayout> GetSnackbarLayouts()
	{
		var layouts = new List<MaterialSnackbar.SnackbarLayout>();
		Collect(Activity.Window!.DecorView, layouts);
		return layouts;
	}

	public static TextView GetMessageView(View snackbarView) => snackbarView.FindViewById<TextView>(Resource.Id.snackbar_text) ?? throw new InvalidOperationException("The snackbar has no message view.");

	public static TextView GetActionView(View snackbarView) => snackbarView.FindViewById<TextView>(Resource.Id.snackbar_action) ?? throw new InvalidOperationException("The snackbar has no action view.");

	public static MaterialSnackbar GetNativeSnackbar(object? platformSnackbar) => platformSnackbar as MaterialSnackbar ?? throw new InvalidOperationException("The toolkit has no platform snackbar.");

	// Waits (up to 10 s) until no snackbar is on screen and returns false. A snackbar that is still there by then cannot be dismissed:
	// its Java peer was disposed while it was shown, so neither the toolkit nor the clean-up can call Dismiss on it, and Material's
	// SnackbarManager still holds it as the current snackbar, so the next Show would dismiss it as "consecutive" and its Dismissed event
	// would reach the next test. Its view is taken out of the hierarchy, and a native snackbar is shown and dismissed so that the
	// SnackbarManager lets go of the stale one (and every callback Material posted for it has run). Returns true in that case.
	public static async Task<bool> RemoveSnackbarsThatCannotBeDismissedAsync()
	{
		if (await AssertHelpers.Wait(() => GetSnackbarLayouts().Count == 0, timeout: 10000))
		{
			return false;
		}

		foreach (var layout in GetSnackbarLayouts())
		{
			(layout.Parent as ViewGroup)?.RemoveView(layout);
		}

		var flush = MaterialSnackbar.Make(Content, "Clean-up", MaterialSnackbar.LengthIndefinite)!;
		flush.Show();
		await AssertHelpers.Wait(() => GetSnackbarLayouts().Count == 1, timeout: 10000);
		flush.Dismiss();
		await AssertHelpers.Wait(() => GetSnackbarLayouts().Count == 0, timeout: 10000);

		return true;
	}

	// Draws the view into a bitmap, as the user sees it (the snackbar view must be attached and laid out).
	public static Bitmap Render(View view)
	{
		var bitmap = Bitmap.CreateBitmap(view.Width, view.Height, Bitmap.Config.Argb8888!)!;
		view.Draw(new Canvas(bitmap));
		return bitmap;
	}

	// Makes Platform.CurrentActivity return null until the returned scope is disposed: Essentials keeps the activity state manager in a
	// private static field whose setter is internal. Callers read AlertsAndroid.Activity before they enter the scope.
	public static IDisposable WithoutCurrentActivity()
	{
		var field = typeof(ActivityStateManager).GetField("defaultImplementation", BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("ActivityStateManager.defaultImplementation was not found.");
		var previous = field.GetValue(null);
		field.SetValue(null, new NoActivityStateManager());
		return new DisposeAction(() => field.SetValue(null, previous));
	}

	static void Collect(View view, List<MaterialSnackbar.SnackbarLayout> layouts)
	{
		if (view is MaterialSnackbar.SnackbarLayout layout)
		{
			layouts.Add(layout);
		}

		if (view is ViewGroup group)
		{
			for (var i = 0; i < group.ChildCount; i++)
			{
				Collect(group.GetChildAt(i)!, layouts);
			}
		}
	}
}

sealed class DisposeAction(Action onDispose) : IDisposable
{
	public void Dispose() => onDispose();
}

// An activity state manager that has no activity, as before the first activity is created.
sealed class NoActivityStateManager : IActivityStateManager
{
	public event EventHandler<ActivityStateChangedEventArgs> ActivityStateChanged
	{
		add { }
		remove { }
	}

	public void Init(Android.App.Application application)
	{
	}

	public void Init(Activity activity, Android.OS.Bundle? bundle)
	{
	}

	public Activity? GetCurrentActivity() => null;

	public Task<Activity> WaitForActivityAsync(CancellationToken cancelToken = default) => throw new NotSupportedException();
}