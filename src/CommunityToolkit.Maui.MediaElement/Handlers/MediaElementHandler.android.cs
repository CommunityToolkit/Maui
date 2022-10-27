using CommunityToolkit.Maui.MediaElement.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>
{
	MediaManager? mediaManager;
	protected override MauiMediaElement CreatePlatformView()
	{
		mediaManager ??= new(MauiContext ?? throw new NullReferenceException(), VirtualView);
		var player = mediaManager.CreatePlatformView();
		return new(Context, player, VirtualView);
	}

	public static void MapIsLooping(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.PlatformView.UpdateIsLooping();
	}

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
		handler.PlatformView?.UpdateStatus();
	}

	public static void MapVolume(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.PlatformView.UpdateVolume();
	}

	public static void MapPlayRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		if (args is not MediaPositionEventArgs)
		{
			return;
		}

		TimeSpan position = ((MediaPositionEventArgs)args).Position;
		handler.PlatformView?.PlayRequested(position);
	}

	public static void MapPauseRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		if (args is not MediaPositionEventArgs)
		{
			return;
		}

		TimeSpan position = ((MediaPositionEventArgs)args).Position;
		handler.PlatformView?.PauseRequested(position);
	}

	public static void MapStopRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		if (args is not MediaPositionEventArgs)
		{
			return;
		}

		TimeSpan position = ((MediaPositionEventArgs)args).Position;
		handler.PlatformView?.StopRequested(position);
	}
}