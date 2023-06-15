namespace CommunityToolkit.Maui.Core.Platform;

/// <summary>
/// Class that hold the method to customize the StatusBar
/// </summary>
public static partial class StatusBar
{
	/// <summary>
	/// Method to change the color of the status bar.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> that will be set to the status bar.</param>
	public static void SetColor(Color? color) =>
		PlatformSetColor(color ?? Colors.Transparent);

	/// <summary>
	/// Method to change the style of the status bar.
	/// </summary>
	/// <param name="statusBarStyle"> The <see cref="StatusBarStyle"/> that will used by status bar.</param>
	public static void SetStyle(StatusBarStyle statusBarStyle) =>
		PlatformSetStyle(statusBarStyle);
}