using Foundation;
using Microsoft.Maui.Platform;
using UIKit;
using Application = Microsoft.Maui.Controls.Application;
using MauiWindow = Microsoft.Maui.Controls.Window;

namespace CommunityToolkit.Maui.Sample;

// Named by UISceneDelegateClassName in Info.plist. The iOS 27 and macOS 27 SDKs terminate an app that does not adopt the UIScene
// life cycle; MauiUISceneDelegate creates the window of each scene from App.CreateWindow.
[Register(nameof(SceneDelegate))]
public class SceneDelegate : MauiUISceneDelegate
{
	public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		base.WillConnect(scene, session, connectionOptions);

		// The Sample has one main window: its pages navigate with Shell.Current, which throws once two Shell windows are active
		// (on Mac Catalyst every visible window is active), and they use Application.Windows[0].
		// Mac Catalyst also connects scenes that the app did not open, e.g. at launch every window that was open when the app quit
		// (the second window of MediaElementMultipleWindowsPage included), and App.CreateWindow gives each of them a new AppShell:
		// close such a second AppShell window before it is drawn.
		if (Window?.GetWindow() is MauiWindow { Page: AppShell } window
			&& Application.Current is Application application
			&& application.Windows.Any(other => other != window && other.Page is AppShell))
		{
			Window.Hidden = true;
			application.CloseWindow(window);
		}
	}
}