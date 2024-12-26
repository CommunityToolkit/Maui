using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using CommunityToolkit.Maui.Core;
using Debug = System.Diagnostics.Debug;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;
using Fragment = AndroidX.Fragment.App.Fragment;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;

namespace CommunityToolkit.Maui.Services;

sealed class DialogFragmentService : IDialogFragmentService
{
	public void OnFragmentAttached(FragmentManager fm, Fragment f, Context context)
	{
	}

	public void OnFragmentCreated(FragmentManager fm, Fragment f, Bundle? savedInstanceState)
	{
	}

	public void OnFragmentDestroyed(FragmentManager fm, Fragment f)
	{
	}

	public void OnFragmentDetached(FragmentManager fm, Fragment f)
	{
	}

	public void OnFragmentPaused(FragmentManager fm, Fragment f)
	{
	}

	public void OnFragmentPreAttached(FragmentManager fm, Fragment f, Context context)
	{
	}

	public void OnFragmentPreCreated(FragmentManager fm, Fragment f, Bundle? savedInstanceState)
	{
	}

	public void OnFragmentResumed(FragmentManager fm, Fragment f)
	{
	}

	public void OnFragmentSaveInstanceState(FragmentManager fm, Fragment f, Bundle outState)
	{
	}

	public void OnFragmentStarted(FragmentManager fm, Fragment f)
	{
		if (!IsDialogFragment(f, out var dialogFragment) || Platform.CurrentActivity is not AppCompatActivity activity)
		{
			return;
		}

		HandleStatusBarColor(dialogFragment, activity);
	}

	static void HandleStatusBarColor(DialogFragment dialogFragment, AppCompatActivity activity)
	{
		if (activity.Window is null)
		{
			return;
		}

		var statusBarColor = activity.Window.StatusBarColor;
		var platformColor = new Android.Graphics.Color(statusBarColor);
		var dialog = dialogFragment.Dialog;

		Debug.Assert(dialog is not null);
		Debug.Assert(dialog.Window is not null);

		var window = dialog.Window;

		bool isColorTransparent = platformColor == Android.Graphics.Color.Transparent;

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var windowInsetsController = window.InsetsController;
			var appearance = activity.Window.InsetsController?.SystemBarsAppearance;

			if (windowInsetsController is null)
			{
				System.Diagnostics.Trace.WriteLine("WindowInsetsController is null, cannot set system bars appearance.");
				return;
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
			window.SetStatusBarColor(platformColor);
			if (!OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				window.SetDecorFitsSystemWindows(!isColorTransparent);
			}
			else
			{
				AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(window, !isColorTransparent);
			}
		}
		else
		{
			dialog.Window.SetStatusBarColor(platformColor);

			if (isColorTransparent)
			{
				window.ClearFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
				window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
			}
			else
			{
				window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
				window.SetFlags(WindowManagerFlags.DrawsSystemBarBackgrounds, WindowManagerFlags.DrawsSystemBarBackgrounds);
			}
		}
	}

	public void OnFragmentStopped(FragmentManager fm, Fragment f)
	{
	}

	public void OnFragmentViewCreated(FragmentManager fm, Fragment f, Android.Views.View v, Bundle? savedInstanceState)
	{
	}

	public void OnFragmentViewDestroyed(FragmentManager fm, Fragment f)
	{
	}

	static bool IsDialogFragment(Fragment fragment, [NotNullWhen(true)] out DialogFragment? dialogFragment)
	{
		dialogFragment = null;
		if (fragment is DialogFragment dialog)
		{
			dialogFragment = dialog;
			return true;
		}
		return false;
	}
}
