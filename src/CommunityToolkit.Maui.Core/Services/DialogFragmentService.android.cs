using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Views;
using AndroidX.AppCompat.App;
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

		var isColorTransparent = platformColor == Android.Graphics.Color.Transparent;

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var windowInsetsController = dialogWindow.InsetsController;
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

			dialogWindow.SetStatusBarColor(platformColor);

			if (!OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				dialogWindow.SetDecorFitsSystemWindows(!isColorTransparent);
			}
			else
			{
				AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(dialogWindow, !isColorTransparent);
			}
		}
		else
		{
			dialogWindow.SetStatusBarColor(platformColor);

			if (isColorTransparent)
			{
				dialogWindow.ClearFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
				dialogWindow.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
			}
			else
			{
				dialogWindow.ClearFlags(WindowManagerFlags.LayoutNoLimits);
				dialogWindow.SetFlags(WindowManagerFlags.DrawsSystemBarBackgrounds, WindowManagerFlags.DrawsSystemBarBackgrounds);
			}
		}
	}
}