namespace CommunityToolkit.Maui.DeviceTests;

// The extension methods with platform code. A feature whose code exists on a single platform (UIViewExtensions: iOS + Mac Catalyst,
// PlatformViewExtensions, AndroidViewGroupExtensions and AndroidWindowExtensions: Android, BrushExtensions: Windows) has tests on that
// platform only. AndroidWindowExtensions.GetCurrentWindow is the window lookup of StatusBar and NavigationBar (Core's WindowExtensions).
// ColorAnimationExtensions is shared code; its tests are here because its animations run on the platform's animation ticker.
public static partial class TestCategory
{
	public const string AndroidViewGroupExtensions = nameof(AndroidViewGroupExtensions);

	public const string AndroidWindowExtensions = nameof(AndroidWindowExtensions);

	public const string BrushExtensions = nameof(BrushExtensions);

	public const string ColorAnimationExtensions = nameof(ColorAnimationExtensions);

	public const string KeyboardExtensions = nameof(KeyboardExtensions);

	public const string PlatformViewExtensions = nameof(PlatformViewExtensions);

	public const string UIViewExtensions = nameof(UIViewExtensions);
}