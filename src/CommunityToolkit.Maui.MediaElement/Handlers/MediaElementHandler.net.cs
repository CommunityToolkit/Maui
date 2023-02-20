using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class MediaElementHandler : ViewHandler<MediaElement, object>
{
	/// <inheritdoc/>
	protected override object CreatePlatformView() => throw new NotImplementedException();

	// Ignoring XML comments for this implementation since it's not used.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public static void MapAspect(object handler, MediaElement MediaElement) => throw new NotImplementedException();
	public static void MapPosition(object handler, MediaElement MediaElement) => throw new NotImplementedException();
	public static void MapShouldKeepScreenOn(object handler, MediaElement MediaElement) => throw new NotImplementedException();
	public static void MapShouldMute(object handler, MediaElement MediaElement) => throw new NotImplementedException();
	public static void MapShouldShowPlaybackControls(object handler, MediaElement MediaElement) => throw new NotImplementedException();
	public static void MapSource(object handler, MediaElement MediaElement) => throw new NotImplementedException();
	public static void MapSpeed(object handler, MediaElement MediaElement) => throw new NotImplementedException();
	public static void MapStatusUpdated(MediaElementHandler handler, MediaElement MediaElement, object? args) => throw new NotImplementedException();
	public static void MapVolume(object handler, MediaElement MediaElement) => throw new NotImplementedException();
	public static void MapPlayRequested(MediaElementHandler handler, MediaElement MediaElement, object? args) => throw new NotImplementedException();
	public static void MapPauseRequested(MediaElementHandler handler, MediaElement MediaElement, object? args) => throw new NotImplementedException();
	public static void MapSeekRequested(MediaElementHandler handler, MediaElement MediaElement, object? args) => throw new NotImplementedException();
	public static void MapStopRequested(MediaElementHandler handler, MediaElement MediaElement, object? args) => throw new NotImplementedException();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}