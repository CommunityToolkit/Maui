using CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;
using Foundation;

namespace CommunityToolkit.Maui.DeviceTests;

// Named by UISceneDelegateClassName in Info.plist. In headless runs it attaches the runner's window to the scene; in the visual
// runner it behaves as MAUI's MauiUISceneDelegate.
[Register("SceneDelegate")]
public class SceneDelegate : MauiTestSceneDelegate
{
}