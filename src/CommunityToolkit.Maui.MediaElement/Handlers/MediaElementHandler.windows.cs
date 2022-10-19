using CommunityToolkit.Maui.MediaElement.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>
{
	protected override MauiMediaElement CreatePlatformView() => new(VirtualView);

	public static void MapPosition(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.PlatformView.UpdatePosition();
	}

	public static void MapShowsPlaybackControls(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.PlatformView.UpdateShowsPlaybackControls();
	}

	public static void MapSource(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.PlatformView.UpdateSource();
	}

	public static void MapSpeed(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.PlatformView.UpdateSpeed();
	}

	public static void MapUpdateStatus(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		handler?.PlatformView.UpdateStatus();
	}

	public static void MapVolume(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.PlatformView.UpdateVolume();
	}
}