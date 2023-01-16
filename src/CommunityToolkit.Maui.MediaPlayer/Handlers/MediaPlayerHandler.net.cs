using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaPlayer;

public partial class MediaPlayerHandler : ViewHandler<MediaPlayer, object>
{
	/// <inheritdoc/>
	protected override object CreatePlatformView() => throw new NotImplementedException();

	// Ignoring XML comments for this implementation since it's not used.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public static void MapPosition(object handler, MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapShouldKeepScreenOn(object handler, MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapShouldShowPlaybackControls(object handler, MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapSource(object handler, MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapSpeed(object handler, MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapStatusUpdated(MediaPlayerHandler handler, MediaPlayer MediaPlayer, object? args) => throw new NotImplementedException();
	public static void MapVolume(object handler, MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapPlayRequested(MediaPlayerHandler handler, MediaPlayer MediaPlayer, object? args) => throw new NotImplementedException();
	public static void MapPauseRequested(MediaPlayerHandler handler, MediaPlayer MediaPlayer, object? args) => throw new NotImplementedException();
	public static void MapSeekRequested(MediaPlayerHandler handler, MediaPlayer MediaPlayer, object? args) => throw new NotImplementedException();
	public static void MapStopRequested(MediaPlayerHandler handler, MediaPlayer MediaPlayer, object? args) => throw new NotImplementedException();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}