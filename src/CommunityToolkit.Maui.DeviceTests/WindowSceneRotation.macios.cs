using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

static class WindowSceneRotation
{
	// Must run on the main thread. onRefused is called with UIKit's reason when iOS 16+ refuses the request.
	public static void Request(UIWindowScene windowScene, UIInterfaceOrientationMask requestedMask, Action<string>? onRefused = null)
	{
		if (OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16))
		{
			windowScene.RequestGeometryUpdate(new UIWindowSceneGeometryPreferencesIOS(requestedMask), error => onRefused?.Invoke(error.LocalizedDescription));
			return;
		}

		// iOS 15 has no RequestGeometryUpdate; apps rotated by writing the device orientation.
		UIDevice.CurrentDevice.SetValueForKey(Foundation.NSNumber.FromNInt((nint)ToDeviceOrientation(requestedMask)), new Foundation.NSString("orientation"));
		UIViewController.AttemptRotationToDeviceOrientation();
	}

	static UIDeviceOrientation ToDeviceOrientation(UIInterfaceOrientationMask mask) => mask switch
	{
		UIInterfaceOrientationMask.Portrait => UIDeviceOrientation.Portrait,
		UIInterfaceOrientationMask.PortraitUpsideDown => UIDeviceOrientation.PortraitUpsideDown,
		// The device and interface landscape orientations are mirrored: a device in LandscapeLeft shows the interface in LandscapeRight.
		UIInterfaceOrientationMask.LandscapeLeft => UIDeviceOrientation.LandscapeRight,
		UIInterfaceOrientationMask.LandscapeRight => UIDeviceOrientation.LandscapeLeft,
		_ => throw new ArgumentOutOfRangeException(nameof(mask), mask, "Rotating on iOS 15 needs a single orientation.")
	};
}
