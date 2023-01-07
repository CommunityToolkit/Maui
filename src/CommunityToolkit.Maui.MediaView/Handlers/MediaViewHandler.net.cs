using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaView;

public partial class MediaViewHandler : ViewHandler<MediaView, object>
{
	/// <inheritdoc/>
	protected override object CreatePlatformView() => throw new NotImplementedException();

	// Ignoring XML comments for this implementation since it's not used.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public static void MapPosition(object handler, MediaView MediaView) => throw new NotImplementedException();
	public static void MapShouldKeepScreenOn(object handler, MediaView MediaView) => throw new NotImplementedException();
	public static void MapShouldShowPlaybackControls(object handler, MediaView MediaView) => throw new NotImplementedException();
	public static void MapSource(object handler, MediaView MediaView) => throw new NotImplementedException();
	public static void MapSpeed(object handler, MediaView MediaView) => throw new NotImplementedException();
	public static void MapStatusUpdated(MediaViewHandler handler, MediaView MediaView, object? args) => throw new NotImplementedException();
	public static void MapVolume(object handler, MediaView MediaView) => throw new NotImplementedException();
	public static void MapPlayRequested(MediaViewHandler handler, MediaView MediaView, object? args) => throw new NotImplementedException();
	public static void MapPauseRequested(MediaViewHandler handler, MediaView MediaView, object? args) => throw new NotImplementedException();
	public static void MapSeekRequested(MediaViewHandler handler, MediaView MediaView, object? args) => throw new NotImplementedException();
	public static void MapStopRequested(MediaViewHandler handler, MediaView MediaView, object? args) => throw new NotImplementedException();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}