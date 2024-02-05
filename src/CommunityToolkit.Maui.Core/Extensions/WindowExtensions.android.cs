using Android.OS;
using Android.Views;

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Provides extension methods for the Window class.
/// </summary>
public static class AndroidWindowExtensions
{
	/// <summary>
	/// Gets the current window associated with the specified activity.
	/// </summary>
	/// <param name="activity">The activity.</param>
	/// <returns>The current window.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the activity window is null.</exception>
	public static Window GetCurrentWindow(this Activity activity)
	{
		var window = activity.Window ?? throw new InvalidOperationException($"{nameof(activity.Window)} cannot be null");
		window.ClearFlags(WindowManagerFlags.TranslucentStatus);
		window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
		return window;
	}


	/// <summary>
	/// Checks if the specified Android version is supported.
	/// </summary>
	/// <param name="version">The Android version to check.</param>
	/// <returns>True if the version is supported; otherwise, false.</returns>
	public static bool IsSupported(BuildVersionCodes version)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast((int)version))
		{
			return true;
		}

		System.Diagnostics.Trace.WriteLine($"This functionality is not available. Minimum supported API is {version}");
		return false;
	}
}
