using System.Reflection;
using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.ApplicationModel;
using MaterialSnackbar = Google.Android.Material.Snackbar.Snackbar;
using View = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

static class AlertsAndroid
{
	public static Activity Activity => Platform.CurrentActivity ?? throw new InvalidOperationException("The test app has no current activity.");

	public static ViewGroup Content => Activity.Window!.DecorView.FindViewById<ViewGroup>(Android.Resource.Id.Content)!;

	public static float Density => Activity.Resources!.DisplayMetrics!.Density;

	// Includes a snackbar that is animating in or out.
	public static List<MaterialSnackbar.SnackbarLayout> GetSnackbarLayouts()
	{
		var layouts = new List<MaterialSnackbar.SnackbarLayout>();
		Collect(Activity.Window!.DecorView, layouts);
		return layouts;
	}

	public static TextView GetMessageView(View snackbarView) => snackbarView.FindViewById<TextView>(Resource.Id.snackbar_text) ?? throw new InvalidOperationException("The snackbar has no message view.");

	public static TextView GetActionView(View snackbarView) => snackbarView.FindViewById<TextView>(Resource.Id.snackbar_action) ?? throw new InvalidOperationException("The snackbar has no action view.");

	public static MaterialSnackbar GetNativeSnackbar(object? platformSnackbar) => platformSnackbar as MaterialSnackbar ?? throw new InvalidOperationException("The toolkit has no platform snackbar.");

	// A snackbar that is still on screen after 10 s cannot be dismissed: its Java peer was disposed while it was shown, so neither the
	// toolkit nor the clean-up can call Dismiss on it, and Material's SnackbarManager still holds it as the current snackbar, so the next
	// Show would dismiss it as "consecutive" and its Dismissed event would reach the next test. Showing and dismissing a native snackbar
	// makes the SnackbarManager let go of the stale one, and by then every callback Material posted for it has run.
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

	public static Bitmap Render(View view)
	{
		var bitmap = Bitmap.CreateBitmap(view.Width, view.Height, Bitmap.Config.Argb8888!)!;
		view.Draw(new Canvas(bitmap));
		return bitmap;
	}

	// Essentials keeps the activity state manager in a private static field whose setter is internal, hence the reflection. Callers read
	// AlertsAndroid.Activity before they enter the scope.
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