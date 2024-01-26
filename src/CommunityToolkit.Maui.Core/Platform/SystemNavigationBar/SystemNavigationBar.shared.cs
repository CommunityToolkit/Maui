namespace CommunityToolkit.Maui.Core.Platform;

/// <summary>
/// Class that hold the method to customize the SystemNavigationBar
/// </summary>
public static partial class SystemNavigationBar
{
	/// <summary>
	/// Method to change the color of the system navigation bar.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> that will be set to the system navigation bar.</param>
	public static void SetColor(Color? color) =>
		PlatformSetColor(color ?? Colors.Transparent);

	/// <summary>
	/// Method to change the style of the system navigation bar.
	/// </summary>
	/// <param name="systemNavigationBarStyle"> The <see cref="SystemNavigationBarStyle"/> that will used by system navigation bar.</param>
	public static void SetStyle(SystemNavigationBarStyle systemNavigationBarStyle) =>
		PlatformSetStyle(systemNavigationBarStyle);
}