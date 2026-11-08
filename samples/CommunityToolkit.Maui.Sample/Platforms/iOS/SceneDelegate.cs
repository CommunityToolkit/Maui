using Foundation;

namespace CommunityToolkit.Maui.Sample;

// Named by UISceneDelegateClassName in Info.plist. The iOS 27 and macOS 27 SDKs terminate an app that does not adopt the UIScene
// life cycle; MauiUISceneDelegate creates the window of each scene from App.CreateWindow.
[Register(nameof(SceneDelegate))]
public class SceneDelegate : MauiUISceneDelegate
{
}