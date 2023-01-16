using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaPlayer;

public partial class MediaPlayerHandler : ViewHandler<Views.MediaPlayer, object>
{
	/// <inheritdoc/>
	protected override object CreatePlatformView() => throw new NotImplementedException();

	// Ignoring XML comments for this implementation since it's not used.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public static void MapPosition(object handler, Views.MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapShouldKeepScreenOn(object handler, Views.MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapShouldShowPlaybackControls(object handler, Views.MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapSource(object handler, Views.MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapSpeed(object handler, Views.MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapStatusUpdated(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer, object? args) => throw new NotImplementedException();
	public static void MapVolume(object handler, Views.MediaPlayer MediaPlayer) => throw new NotImplementedException();
	public static void MapPlayRequested(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer, object? args) => throw new NotImplementedException();
	public static void MapPauseRequested(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer, object? args) => throw new NotImplementedException();
	public static void MapSeekRequested(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer, object? args) => throw new NotImplementedException();
	public static void MapStopRequested(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer, object? args) => throw new NotImplementedException();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}