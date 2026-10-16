namespace CommunityToolkit.Maui.DeviceTests;

// MediaElementAndroid: the Media3/ExoPlayer manager, view, handler, media session service and data sources. MediaElementApple: the
// iOS and Mac Catalyst implementation (src/CommunityToolkit.Maui.MediaElement/**/*.macios.cs). Windows has one category per source
// file (the large MediaManager split in three by topic), so that a filtered run, which launches the app once per category, stays
// well inside the time limit of a category and "TestFilter=Category=<X>" reproduces a failure of one file with one command; its
// tests compile for Windows only, the constants everywhere.
public static partial class TestCategory
{
	public const string MediaElementAndroid = nameof(MediaElementAndroid);

	public const string MediaElementApple = nameof(MediaElementApple);

	// Views/MediaManager.windows.cs: property mapping (aspect, controls, loop, volume, mute, keep-screen-on), the "no platform view" guards, Dispose.
	public const string MediaManagerWindows = nameof(MediaManagerWindows);

	// Views/MediaManager.windows.cs: playback, seek, speed, position/status updates, native-event handlers and failure reporting.
	public const string MediaManagerPlaybackWindows = nameof(MediaManagerPlaybackWindows);

	// Views/MediaManager.windows.cs (and the #if WINDOWS parts of MediaManager.shared.cs): every source type, headers and adaptive streaming.
	public const string MediaManagerSourceWindows = nameof(MediaManagerSourceWindows);

	// Handlers/MediaElementHandler.windows.cs (and the #if WINDOWS parts of MediaElementHandler.shared.cs), including the leak tests.
	public const string MediaElementHandlerWindows = nameof(MediaElementHandlerWindows);

	// Views/MauiMediaElement.windows.cs.
	public const string MauiMediaElementWindows = nameof(MauiMediaElementWindows);

	// Primitives/CustomTransportControls.windows.cs.
	public const string CustomTransportControlsWindows = nameof(CustomTransportControlsWindows);

	// Primitives/HttpRandomAccessStream.windows.cs.
	public const string HttpRandomAccessStreamWindows = nameof(HttpRandomAccessStreamWindows);

	// Primitives/Metadata.windows.cs and MediaManager's UpdateMetadata.
	public const string MediaMetadataWindows = nameof(MediaMetadataWindows);

	// Services/AppPackageService.windows.cs.
	public const string AppPackageServiceWindows = nameof(AppPackageServiceWindows);
}