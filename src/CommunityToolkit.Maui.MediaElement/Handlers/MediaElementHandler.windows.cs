using System;
using CommunityToolkit.Maui.MediaElement.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>
{
	MediaManager? mediaManager;
	protected override MauiMediaElement CreatePlatformView()
	{
		mediaManager ??= new(this.MauiContext ?? throw new NullReferenceException(), VirtualView);
		var mediaPlatform = mediaManager.CreatePlatformView();
		return new(mediaPlatform);
	}

	public static void MapPosition(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.mediaManager?.UpdatePosition();
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

		TimeSpan position = ((MediaPositionEventArgs)args).Position;
		handler.mediaManager?.Play(position);
	}

	public static void MapPauseRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		if (args is not MediaPositionEventArgs)
		{
			return;
		}

		TimeSpan position = ((MediaPositionEventArgs)args).Position;
		handler.mediaManager?.Pause(position);
	}

	public static void MapStopRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		if (args is not MediaPositionEventArgs)
		{
			return;
		}

		TimeSpan position = ((MediaPositionEventArgs)args).Position;
		handler.mediaManager?.Stop(position);
	}
}