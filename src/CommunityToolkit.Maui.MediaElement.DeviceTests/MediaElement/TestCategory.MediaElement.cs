namespace CommunityToolkit.Maui.DeviceTests;

// Windows has one category per source file (the large MediaManager is split in three by topic): a filtered run launches the app once per
// category, so small categories keep each launch well inside the time limit, and "TestFilter=Category=<X>" reproduces a failure of one file
// with one command.
public static partial class TestCategory
{
	public const string MediaElementAndroid = nameof(MediaElementAndroid);

	public const string MediaElementApple = nameof(MediaElementApple);

	public const string MediaManagerWindows = nameof(MediaManagerWindows);

	public const string MediaManagerPlaybackWindows = nameof(MediaManagerPlaybackWindows);

	public const string MediaManagerSourceWindows = nameof(MediaManagerSourceWindows);

	public const string MediaElementHandlerWindows = nameof(MediaElementHandlerWindows);

	public const string MauiMediaElementWindows = nameof(MauiMediaElementWindows);

	public const string CustomTransportControlsWindows = nameof(CustomTransportControlsWindows);

	public const string HttpRandomAccessStreamWindows = nameof(HttpRandomAccessStreamWindows);

	public const string MediaMetadataWindows = nameof(MediaMetadataWindows);

	public const string AppPackageServiceWindows = nameof(AppPackageServiceWindows);
}