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
}