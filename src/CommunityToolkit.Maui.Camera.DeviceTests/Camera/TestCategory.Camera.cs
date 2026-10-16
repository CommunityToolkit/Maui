namespace CommunityToolkit.Maui.DeviceTests;

// The camera on Android has one category per source file, each named after the file and ending with the platform. CameraApple covers
// the camera code that exists only on iOS and Mac Catalyst (CameraManager, CameraProvider and CameraViewExtensions .macios.cs and the
// IOS/MACCATALYST blocks of the shared files), CameraWindows the camera code that exists only on Windows (the .windows.cs files and
// the WINDOWS blocks of the shared files). Android is split because its tests drive the emulator's cameras through CameraX and take
// minutes, so "TestFilter=Category=<X>" reproduces a failure of one file with one short run; the Apple and Windows categories run in
// seconds.
public static partial class TestCategory
{
	// CameraViewExtensions.android.cs: flash mode conversion and the camera availability check.
	public const string CameraViewExtensionsAndroid = nameof(CameraViewExtensionsAndroid);

	// CameraInfo (the Android CameraSelector part).
	public const string CameraInfoAndroid = nameof(CameraInfoAndroid);

	// CameraProvider.android.cs and CameraProvider.shared.cs.
	public const string CameraProviderAndroid = nameof(CameraProviderAndroid);

	// CameraManager.android.cs and CameraManager.shared.cs (CameraX use cases, sessions, video recording).
	public const string CameraManagerAndroid = nameof(CameraManagerAndroid);

	// CameraViewHandler on Android.
	public const string CameraViewHandlerAndroid = nameof(CameraViewHandlerAndroid);

	// CameraView on Android (SetExtensionMode and the view's use of the handler and the manager).
	public const string CameraViewAndroid = nameof(CameraViewAndroid);

	public const string CameraApple = nameof(CameraApple);

	public const string CameraWindows = nameof(CameraWindows);
}