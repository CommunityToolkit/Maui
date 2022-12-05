using AVFoundation;
using AVKit;
using CoreFoundation;
using CoreMedia;
using Foundation;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaManager : IDisposable
{
	const NSKeyValueObservingOptions valueObserverOptions =
		NSKeyValueObservingOptions.Initial | NSKeyValueObservingOptions.New;

	protected NSObject? playedToEndObserver;
	protected NSObject? itemFailedToPlayToEndTimeObserver;
	protected NSObject? playbackStalledObserver;
	protected NSObject? errorObserver;
	protected IDisposable? statusObserver;
	protected IDisposable? timeControlStatusObserver;
	protected IDisposable? currentItemErrorObserver;
	protected IDisposable? currentItemStatusObserver;
	protected AVPlayerViewController? playerViewController;
	protected AVPlayerItem? playerItem;

	public (PlatformMediaView player, AVPlayerViewController playerViewController) CreatePlatformView()
	{
		player = new();
		playerViewController = new()
		{
			Player = player
		};

		AddStatusObservers();
		AddPlayedToEndObserver();
		AddErrorObservers();

		return (player, playerViewController);
	}

	protected virtual partial void PlatformPlay(TimeSpan timeSpan)
	{
		player?.Play();
	}

	protected virtual partial void PlatformPause(TimeSpan timeSpan)
	{
		player?.Pause();
	}

	protected virtual partial void PlatformStop(TimeSpan timeSpan)
	{
		// There's no Stop method so pause the video and reset its position
		player?.Seek(CMTime.Zero);
		player?.Pause();

		mediaElement.CurrentStateChanged(MediaElementState.Stopped);
	}

	protected virtual partial void PlatformUpdateSource()
	{
		AVAsset? asset = null;

		if (mediaElement.Source is UriMediaSource)
		{
			string uri = (mediaElement.Source as UriMediaSource)!.Uri!.AbsoluteUri;

			if (!string.IsNullOrWhiteSpace(uri))
			{
				asset = AVAsset.FromUrl(new NSUrl(uri));
			}
		}
		else if (mediaElement.Source is FileMediaSource)
		{
			string uri = (mediaElement.Source as FileMediaSource)!.File!;

			if (!string.IsNullOrWhiteSpace(uri))
			{
				asset = AVAsset.FromUrl(NSUrl.CreateFileUrl(new[] { uri }));
			}
		}

		if (asset is not null)
		{
			playerItem = new AVPlayerItem(asset);
		}
		else
		{
			playerItem = null;
		}

		currentItemErrorObserver?.Dispose();

		player?.ReplaceCurrentItemWithPlayerItem(playerItem);

		currentItemErrorObserver = playerItem?.AddObserver("error",
			valueObserverOptions, (NSObservedChange change) =>
		{
			if (player?.CurrentItem?.Error != null)
			{
				mediaElement.MediaFailed(
					new MediaFailedEventArgs(player?.CurrentItem?.Error?.LocalizedDescription ?? ""));
			}
		});

		if (playerItem is not null && playerItem.Error is null)
		{
			mediaElement.MediaOpened();

			if (mediaElement.AutoPlay)
			{
				player?.Play();
			}
		}
	}

	protected virtual partial void PlatformUpdateSpeed()
	{
		if (playerViewController?.Player is null)
		{
			return;
		}

		playerViewController.Player.Rate = (float)mediaElement.Speed;
	}

	protected virtual partial void PlatformUpdateShowsPlaybackControls()
	{
		if (playerViewController is null)
		{
			return;
		}

		playerViewController.ShowsPlaybackControls =
			mediaElement.ShowsPlaybackControls;
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (player is null)
		{
			return;
		}

		var controlPosition = ConvertTime(player.CurrentTime);
		if (Math.Abs((controlPosition - mediaElement.Position).TotalSeconds) > 1)
		{
			player.Seek(CMTime.FromSeconds(mediaElement.Position.TotalSeconds, 1),
				(complete) =>
				{
					if (complete)
					{
						mediaElement?.SeekCompleted();
					}
				});
		}
	}

	protected virtual partial void PlatformUpdateStatus()
	{
		if (player is null)
		{
			return;
		}

		if (playerItem is not null)
		{
			mediaElement.Duration = ConvertTime(playerItem.Duration);
			mediaElement.Position = ConvertTime(playerItem.CurrentTime);
		}
		else
		{
			player.Pause();
			mediaElement.Duration = mediaElement.Position = TimeSpan.Zero;
		}
	}

	protected virtual partial void PlatformUpdateVolume()
	{
		if (playerViewController?.Player is null)
		{
			return;
		}

		playerViewController.Player.Volume = (float)mediaElement.Volume;
	}

	protected virtual partial void PlatformUpdateKeepScreenOn()
	{
		if (player is null || mediaElement is null)
		{
			return;
		}

		player.PreventsDisplaySleepDuringVideoPlayback = mediaElement.KeepScreenOn;
	}

	protected virtual partial void PlatformUpdateIsLooping()
	{
		// no-op
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (player is not null)
			{
				DestroyErrorObservers();
				DestroyPlayedToEndObserver();

				currentItemErrorObserver?.Dispose();
				player.ReplaceCurrentItemWithPlayerItem(null);
				statusObserver?.Dispose();
				timeControlStatusObserver?.Dispose();
				player.Dispose();
			}

			playerViewController?.Dispose();
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	static TimeSpan ConvertTime(CMTime cmTime)
	{
		return TimeSpan.FromSeconds(
			double.IsNaN(cmTime.Seconds) ? 0 : cmTime.Seconds);
	}

	void AddStatusObservers()
	{
		if (player is null)
		{
			return;
		}

		statusObserver = player.AddObserver("status", valueObserverOptions, StatusChanged);
		timeControlStatusObserver = player.AddObserver("timeControlStatus",
			valueObserverOptions, TimeControlStatusChanged);
	}

	void AddErrorObservers()
	{
		DestroyErrorObservers();

		itemFailedToPlayToEndTimeObserver = AVPlayerItem.Notifications.ObserveItemFailedToPlayToEndTime(ErrorOccured);
		playbackStalledObserver = AVPlayerItem.Notifications.ObservePlaybackStalled(ErrorOccured);
		errorObserver = AVPlayerItem.Notifications.ObserveNewErrorLogEntry(ErrorOccured);
	}

	void AddPlayedToEndObserver()
	{
		DestroyPlayedToEndObserver();

		playedToEndObserver = AVPlayerItem.Notifications.ObserveDidPlayToEndTime(PlayedToEnd);
	}

	void DestroyErrorObservers()
	{
		itemFailedToPlayToEndTimeObserver?.Dispose();
		playbackStalledObserver?.Dispose();
		errorObserver?.Dispose();
	}

	void DestroyPlayedToEndObserver()
	{
		playedToEndObserver?.Dispose();
	}

	void StatusChanged(NSObservedChange obj)
	{
		if (player is null)
		{
			return;
		}

		MediaElementState newState = mediaElement.CurrentState;

		switch (player.Status)
		{
			case AVPlayerStatus.Unknown:
				newState = MediaElementState.Stopped;
				break;
			case AVPlayerStatus.ReadyToPlay:
				newState = MediaElementState.Paused;
				break;
			case AVPlayerStatus.Failed:
				newState = MediaElementState.Failed;
				break;
		}

		mediaElement.CurrentStateChanged(newState);
	}

	void TimeControlStatusChanged(NSObservedChange obj)
	{
		if (player is null)
		{
			return;
		}

		if (player.Status == AVPlayerStatus.Unknown)
		{
			return;
		}

		if (player.CurrentItem?.Error is not null)	{ return; }

		MediaElementState newState = mediaElement.CurrentState;

		switch (player.TimeControlStatus)
		{
			case AVPlayerTimeControlStatus.Paused:
				newState = MediaElementState.Paused;
				break;
			case AVPlayerTimeControlStatus.Playing:
				newState = MediaElementState.Playing;
				break;
			case AVPlayerTimeControlStatus.WaitingToPlayAtSpecifiedRate:
				newState = MediaElementState.Buffering;
				break;
		}

		mediaElement.CurrentStateChanged(newState);
	}

	void ErrorOccured(object? sender, NSNotificationEventArgs args)
	{
		string message;
		
		var error = player?.CurrentItem?.Error;
		if (error != null)
		{
			message = error.LocalizedDescription;
			
			mediaElement.MediaFailed(new MediaFailedEventArgs(message));
		}
		else
		{
			// Non-fatal error, TODO log?
			message = args.Notification?.ToString() ?? "MediaItem failed with unknown reason";
		}
	}

	void PlayedToEnd(object? sender, NSNotificationEventArgs args)
	{
		if (args.Notification.Object != playerViewController?.Player?.CurrentItem || player is null)
		{
			return;
		}

		if (mediaElement.IsLooping)
		{
			playerViewController?.Player?.Seek(CMTime.Zero);
			player.Play();
		}
		else
		{
			try
			{
				DispatchQueue.MainQueue.DispatchAsync(mediaElement.MediaEnded);
			}
			catch (Exception e)
			{
				// TODO inject ILogger everywhere and report there?
				//Log.Warning("MediaElement", $"Failed to play media to end: {e}");
			}
		}
	}
}
