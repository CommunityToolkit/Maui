namespace CommunityToolkit.Maui.DeviceTests;

public static partial class TestCategory
{
	public const string AndroidViewGroupExtensions = nameof(AndroidViewGroupExtensions);

	public const string AndroidWindowExtensions = nameof(AndroidWindowExtensions);

	public const string BrushExtensions = nameof(BrushExtensions);

	// Shared code, but its animations run on the platform's animation ticker, so its tests run on devices.
	public const string ColorAnimationExtensions = nameof(ColorAnimationExtensions);

	// Shared code, but each platform ships its own globalization data, so its culture behavior is proven on devices.
	public const string ColorConversionExtensions = nameof(ColorConversionExtensions);

	public const string KeyboardExtensions = nameof(KeyboardExtensions);

	public const string PlatformViewExtensions = nameof(PlatformViewExtensions);

	public const string UIViewExtensions = nameof(UIViewExtensions);
}