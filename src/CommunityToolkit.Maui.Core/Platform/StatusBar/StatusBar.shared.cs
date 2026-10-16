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
	/// <remarks>
	/// <para>
	/// iOS, .NET 10: sets the style with UIApplication.SetStatusBarStyle, which a scene-based app (its Info.plist has a UIApplicationSceneManifest, which
	/// every app built with the Xcode 27 SDK needs) ignores, and which iOS 27 reports as a no-op: the status bar keeps the style that the view
	/// controllers of the app return.
	/// </para>
	/// <para>
	/// iOS, .NET 11: sets <c>Window.StatusBarTheme</c> of every window that the app has at the time of the call, and the view controllers of .NET MAUI
	/// return the matching style: <see cref="StatusBarStyle.LightContent"/> sets <c>StatusBarTheme.Dark</c>, <see cref="StatusBarStyle.DarkContent"/>
	/// sets <c>StatusBarTheme.Light</c> and <see cref="StatusBarStyle.Default"/> sets <c>StatusBarTheme.Default</c>. A window that opens later keeps its
	/// own <c>StatusBarTheme</c>. Needs <c>UseMauiCommunityToolkit()</c> in MauiProgram.cs; without it the call only writes a trace message.
	/// UIKit applies the style only when the app's Info.plist does not set <c>UIViewControllerBasedStatusBarAppearance</c> to false: with false the
	/// status bar keeps its style and the call also writes a trace message. Remove that key, which the .NET 10 setup of StatusBarBehavior adds.
	/// Call it on the main thread.
	/// </para>
	/// </remarks>
	public static void SetStyle(StatusBarStyle statusBarStyle) =>
		PlatformSetStyle(statusBarStyle);
}