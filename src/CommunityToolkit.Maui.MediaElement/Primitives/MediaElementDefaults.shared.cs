namespace CommunityToolkit.Maui.Core;

static class MediaElementDefaults
{
	public const Aspect Aspect = Microsoft.Maui.Aspect.AspectFit;

	public const int MediaHeight = 0;

	public const int MediaWidth = 0;

	public const bool ShouldAutoPlay = false;

	public const bool ShouldLoopPlayback = false;

	public const bool ShouldKeepScreenOn = false;

	public const bool ShouldMute = false;

	public const bool ShouldShowPlaybackControls = false;

	public const double Speed = 1.0;

	public const double Volume = 1.0;

	public const string MetadataTitle = "";

	public const string MetadataArtist = "";

	public const string MetadataArtworkUrl = "";

	public const MediaElementState CurrentState = MediaElementState.None;

	public static TimeSpan Position { get; } = TimeSpan.Zero;

	public static TimeSpan Duration { get; } = TimeSpan.Zero;
}