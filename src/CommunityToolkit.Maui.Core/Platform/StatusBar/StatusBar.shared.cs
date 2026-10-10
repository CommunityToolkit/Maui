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
	/// iOS, .NET 10: sets the style of the whole app: the status bar of every window shows it, also of the windows that open later. The first
	/// <see cref="StatusBarStyle.LightContent"/> or <see cref="StatusBarStyle.DarkContent"/> replaces, through the public Objective-C runtime API, the
	/// implementation of <c>preferredStatusBarStyle</c> of UIViewController and of the UIKit containers that have their own, until the app ends: every
	/// view controller that does not choose a style returns the requested one. Those are the view controllers of .NET MAUI and the UIKit view
	/// controllers that apps present, such as Safari, the photo, image, contact and document pickers, message compose and event edit. View controllers
	/// that override <c>preferredStatusBarStyle</c> keep their own style, and an override that returns the base implementation gets the requested style.
	/// <see cref="StatusBarStyle.Default"/> gives every view controller its own style back. On iPhone a page sheet or a form sheet shows the light content
	/// that UIKit chooses for it. Unlike the <c>Window.StatusBarTheme</c> of .NET 11, which each window has, the style belongs to the app. UIKit takes
	/// the style from the view controllers only when the app's Info.plist does not set <c>UIViewControllerBasedStatusBarAppearance</c> to false. With
	/// false (the .NET 10 setup of StatusBarBehavior) the call sets the style of the whole app with UIApplication.SetStatusBarStyle instead: UIKit
	/// applies it in an app built with an iOS SDK before 27.0. In an app built with a later SDK the status bar keeps its style and the call writes a
	/// trace message: remove the key. Call it on the main thread: a call from another thread throws <c>UIKitThreadAccessException</c>, also in a
	/// Release build.
	/// </para>
	/// <para>
	/// iOS, .NET 11: sets <c>Window.StatusBarTheme</c> of every window that the app has at the time of the call, and the view controllers of .NET MAUI
	/// return the matching style: <see cref="StatusBarStyle.LightContent"/> sets <c>StatusBarTheme.Dark</c>, <see cref="StatusBarStyle.DarkContent"/>
	/// sets <c>StatusBarTheme.Light</c> and <see cref="StatusBarStyle.Default"/> sets <c>StatusBarTheme.Default</c>. A window that opens later keeps its
	/// own <c>StatusBarTheme</c>. Setting the theme needs <c>UseMauiCommunityToolkit()</c> in MauiProgram.cs. UIKit applies the theme only when the
	/// app's Info.plist does not set <c>UIViewControllerBasedStatusBarAppearance</c> to false; then, without <c>UseMauiCommunityToolkit()</c>, the call
	/// only writes a trace message. With false (the .NET 10 setup of StatusBarBehavior) the call also sets the style of the whole app with
	/// UIApplication.SetStatusBarStyle, as on .NET 10: UIKit applies it in an app built with an iOS SDK before 27.0. In an app built with a later SDK
	/// the status bar keeps its style and the call writes a trace message: remove the key. Call it on the main thread.
	/// </para>
	/// </remarks>
	public static void SetStyle(StatusBarStyle statusBarStyle) =>
		PlatformSetStyle(statusBarStyle);
}