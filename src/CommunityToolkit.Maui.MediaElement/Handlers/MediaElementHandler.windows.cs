using CommunityToolkit.Maui.MediaElement.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>, IDisposable
{
	protected override MauiMediaElement CreatePlatformView()
	{
		mediaManager ??= new(this.MauiContext ?? throw new NullReferenceException(), VirtualView);
		var mediaPlatform = mediaManager.CreatePlatformView();
		return new(mediaPlatform);
	}

	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		platformView.Dispose();
		Dispose();
		base.DisconnectHandler(platformView);
	}

	public static void MapIsLooping(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.mediaManager?.UpdateIsLooping();
	}

	public static void MapShowsPlaybackControls(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.mediaManager?.UpdateShowsPlaybackControls();
	}

	public static void MapSource(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.mediaManager?.UpdateSource();
	}

	public static void MapSpeed(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.mediaManager?.UpdateSpeed();
	}

	public static void MapUpdateStatus(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		handler?.mediaManager?.UpdateStatus();
	}

	public static void MapVolume(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.mediaManager?.UpdateVolume();
	}

	public static void MapPlayRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		if (args is not MediaPositionEventArgs)
		{
			return;
		}

		handler.mediaManager?.Play();
	}

	public static void MapPauseRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		handler.mediaManager?.Pause();
	}

	public static void MapSeekRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		if (args is not SeekRequestedEventArgs positionArgs)
		{
			return;
		}

		handler.mediaManager?.Seek(positionArgs.RequestedPosition);
	}

	public static void MapStopRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		handler.mediaManager?.Stop();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			mediaManager?.Dispose();
			mediaManager = null;
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}