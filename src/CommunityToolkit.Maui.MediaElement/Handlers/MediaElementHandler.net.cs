using System;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler : ViewHandler<MediaElement, object>
{
	protected override object CreatePlatformView() => throw new NotImplementedException();

	public static void MapPosition(object handler, MediaElement mediaElement) { }
	public static void MapShowsPlaybackControls(object handler, MediaElement mediaElement) { }
	public static void MapSource(object handler, MediaElement mediaElement) { }
	public static void MapSpeed(object handler, MediaElement mediaElement) { }
	public static void MapUpdateStatus(MediaElementHandler handler, MediaElement mediaElement, object? args) { }
	public static void MapVolume(object handler, MediaElement mediaElement) { }
	public static void MapPlayRequested(MediaElementHandler handler, MediaElement mediaElement, object? args) { }
	public static void MapPauseRequested(MediaElementHandler handler, MediaElement mediaElement, object? args) { }
	public static void MapStopRequested(MediaElementHandler handler, MediaElement mediaElement, object? args) { }
}