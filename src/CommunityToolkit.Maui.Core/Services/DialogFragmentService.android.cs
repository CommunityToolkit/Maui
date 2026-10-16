using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Android.Content;
using Android.Views;
using AndroidX.AppCompat.App;
using CommunityToolkit.Maui.Core.Platform;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;
using Fragment = AndroidX.Fragment.App.Fragment;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;

namespace CommunityToolkit.Maui.Core.Services;

sealed partial class DialogFragmentService : IDialogFragmentService
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentAttached(FragmentManager fm, Fragment f, Context context)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentCreated(FragmentManager fm, Fragment f, Bundle? savedInstanceState)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentDestroyed(FragmentManager fm, Fragment f)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentDetached(FragmentManager fm, Fragment f)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentPaused(FragmentManager fm, Fragment f)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentPreAttached(FragmentManager fm, Fragment f, Context context)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentPreCreated(FragmentManager fm, Fragment f, Bundle? savedInstanceState)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentResumed(FragmentManager fm, Fragment f)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentSaveInstanceState(FragmentManager fm, Fragment f, Bundle outState)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentStarted(FragmentManager fm, Fragment f)
	{
		if (!TryConvertToDialogFragment(f, out var dialogFragment) || Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not AppCompatActivity activity)
		{
			return;
		}

		HandleStatusBarColor(dialogFragment, activity);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentStopped(FragmentManager fm, Fragment f)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentViewCreated(FragmentManager fm, Fragment f, Android.Views.View v, Bundle? savedInstanceState)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFragmentViewDestroyed(FragmentManager fm, Fragment f)
	{
	}

	static bool TryConvertToDialogFragment(Fragment fragment, [NotNullWhen(true)] out DialogFragment? dialogFragment)
	{
		dialogFragment = null;

		if (fragment is not DialogFragment dialog)
		{
			return false;
		}

		dialogFragment = dialog;
		return true;
	}

	static void HandleStatusBarColor(DialogFragment dialogFragment, AppCompatActivity activity)
	{
		if (activity.Window is null)
		{
			return;
		}

		var statusBarColor = activity.Window.StatusBarColor;
		var platformColor = new Android.Graphics.Color(statusBarColor);
		if (dialogFragment.Dialog?.Window is not Window dialogWindow)
		{
			throw new InvalidOperationException("Dialog window cannot be null");
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			HandleStatusBarColor_AndroidApi35AndHigher(dialogWindow, activity.Window, activity, platformColor);
		}
		else if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			HandleStatusBarColor_AndroidApi30To34(dialogWindow, activity, platformColor);
		}
		else if (OperatingSystem.IsAndroidVersionAtLeast(21))
		{
			HandleStatusBarColor_AndroidApiLessThan30(dialogWindow, platformColor);
		}
		else
		{
			throw new NotSupportedException($"{nameof(DialogFragmentService)} requires Android API 21 or higher");
		}
	}

	// LayoutNoLimits is cleared because it pushed modal content behind the system bars for the default transparent color (#2351).
	[SupportedOSPlatform("android21.0"), UnsupportedOSPlatform("android30.0")]
	static void HandleStatusBarColor_AndroidApiLessThan30(in Window dialogWindow, in Android.Graphics.Color platformColor)
	{
		dialogWindow.SetStatusBarColor(platformColor);
		dialogWindow.ClearFlags(WindowManagerFlags.LayoutNoLimits);
		dialogWindow.SetFlags(WindowManagerFlags.DrawsSystemBarBackgrounds, WindowManagerFlags.DrawsSystemBarBackgrounds);
		AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(dialogWindow, false);
	}

	[SupportedOSPlatform("android30.0"), UnsupportedOSPlatform("android35.0")]
	static void HandleStatusBarColor_AndroidApi30To34(in Window dialogWindow, in AppCompatActivity activity, in Android.Graphics.Color platformColor)
	{
		if (!TrySetSystemBarsAppearance(dialogWindow, activity, platformColor))
		{
			return;
		}

		dialogWindow.SetStatusBarColor(platformColor);
		AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(dialogWindow, false);
	}

	// On API 35+, StatusBar.SetColor draws an overlay view on top of the window's status bar color, which Android only draws for apps that target API 34 or lower.
	[SupportedOSPlatform("android35.0")]
	static void HandleStatusBarColor_AndroidApi35AndHigher(in Window dialogWindow, in Window hostWindow, in AppCompatActivity activity, in Android.Graphics.Color platformColor)
	{
		if (!TrySetSystemBarsAppearance(dialogWindow, activity, platformColor))
		{
			return;
		}

#pragma warning disable CA1422 // Obsoleted on API 35+, but still honored for apps that target API 34 or lower
		dialogWindow.SetStatusBarColor(platformColor);
#pragma warning restore CA1422

		// A floating dialog, like a picker, does not reach the status bar, so the overlay would cover its content.
		// The fragment also starts each time the app returns to the foreground, and by then the modal page may have set its own color.
		if (!dialogWindow.IsFloating && StatusBar.TryGetOverlayColor(hostWindow, out var hostOverlayColor))
		{
			StatusBar.AddOverlayIfMissing(dialogWindow, hostOverlayColor);
		}

		AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(dialogWindow, false);
	}

	[SupportedOSPlatform("android30.0")]
	static bool TrySetSystemBarsAppearance(in Window dialogWindow, in AppCompatActivity activity, in Android.Graphics.Color platformColor)
	{
		var isColorTransparent = platformColor == Android.Graphics.Color.Transparent;
		var windowInsetsController = dialogWindow.InsetsController;
		var appearance = activity.Window?.InsetsController?.SystemBarsAppearance;

		if (windowInsetsController is null)
		{
			System.Diagnostics.Trace.WriteLine("WindowInsetsController is null, cannot set system bars appearance.");
			return false;
		}

		if (appearance.HasValue)
		{
			windowInsetsController.SetSystemBarsAppearance(appearance.Value, appearance.Value);
		}
		else
		{
			windowInsetsController.SetSystemBarsAppearance(
				isColorTransparent ? 0 : (int)WindowInsetsControllerAppearance.LightStatusBars,
				(int)WindowInsetsControllerAppearance.LightStatusBars);
		}

		return true;
	}
}