using System.Diagnostics;
using Android.Views;
using Microsoft.Maui.Platform;
using Activity = Android.App.Activity;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Provides extension methods for the Window class.
/// </summary>
public static class AndroidWindowExtensions
{
	/// <summary>
	/// Gets the current visible window associated with the specified activity.
	/// </summary>
	/// <param name="activity">The activity.</param>
	/// <returns>The current window.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the activity window is null.</exception>
	public static Window GetCurrentWindow(this Activity activity)
	{
		Window? currentWindow = null;

		var fragmentManager = activity.GetFragmentManager();
		if (fragmentManager is not null && fragmentManager.Fragments.OfType<DialogFragment>().Any())
		{
			var fragments = fragmentManager.Fragments;
			for (var i = fragments.Count - 1; i >= 0; i--)
			{
				if (fragments[i] is DialogFragment { Dialog: { IsShowing: true, Window: not null }, IsVisible: true } dialogFragment)
				{
					currentWindow = dialogFragment.Dialog.Window;
					break;
				}
			}
		}

		currentWindow ??= activity.Window ?? throw new InvalidOperationException($"{nameof(activity.Window)} cannot be null");

		currentWindow.ClearFlags(WindowManagerFlags.TranslucentStatus);
		currentWindow.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
		
		return currentWindow;
	}
}