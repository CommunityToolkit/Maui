namespace CommunityToolkit.Maui.DeviceTests;

// Android is split into one category per source file because its tests drive the emulator's cameras through CameraX and take minutes, so
// "TestFilter=Category=<X>" reproduces a failure of one file with one short run. The Apple and Windows categories run in seconds.
public static partial class TestCategory
{
	public const string CameraViewExtensionsAndroid = nameof(CameraViewExtensionsAndroid);

	public const string CameraInfoAndroid = nameof(CameraInfoAndroid);

	public const string CameraProviderAndroid = nameof(CameraProviderAndroid);

	public const string CameraManagerAndroid = nameof(CameraManagerAndroid);

	public const string CameraViewHandlerAndroid = nameof(CameraViewHandlerAndroid);

	public const string CameraViewAndroid = nameof(CameraViewAndroid);

	public const string CameraSwitchStopAndroid = nameof(CameraSwitchStopAndroid);

	public const string CameraPermissionResumeAndroid = nameof(CameraPermissionResumeAndroid);

	public const string CameraApple = nameof(CameraApple);

	public const string CameraWindows = nameof(CameraWindows);
}