using AVFoundation;
using AVKit;
using CoreFoundation;
using CoreMedia;
using Foundation;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaManager : IDisposable
{
	const NSKeyValueObservingOptions valueObserverOptions =
		NSKeyValueObservingOptions.Initial | NSKeyValueObservingOptions.New;

	NSObject? playedToEndObserver;
	NSObject? itemFailedToPlayToEndTimeObserver;
	NSObject? playbackStalledObserver;
	NSObject? errorObserver;
	IDisposable? statusObserver;
	IDisposable? volumeObserver;
	IDisposable? rateObserver;
	IDisposable? timeControlStatusObserver;
	IDisposable? currentItemErrorObserver;
	AVPlayerViewController? playerViewController;
	AVPlayerItem? playerItem;

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

	protected virtual partial void PlatformPlay()
	{
		if (player?.CurrentTime == playerItem?.Duration)
		{
			return;
		}

		player?.Play();
	}

	protected virtual partial void PlatformPause()
	{
		player?.Pause();
	}

	protected virtual partial void PlatformSeek(TimeSpan position)
	{
		if (playerItem is null || player?.CurrentItem is null
			|| player?.Status != AVPlayerStatus.ReadyToPlay)
		{
			return;
		}

		var ranges = player.CurrentItem.SeekableTimeRanges;
		var seekTo = new CMTime(Convert.ToInt64(position.TotalMilliseconds), 1000);
		foreach (var v in ranges)
		{
			if (seekTo >= (seekTo - v.CMTimeRangeValue.Start)
				&& seekTo < (v.CMTimeRangeValue.Start + v.CMTimeRangeValue.Duration))
			{
				player.Seek(seekTo + v.CMTimeRangeValue.Start, (complete) =>
				{
					if (complete)
					{
						mediaElement?.SeekCompleted();
					}
				});
				break;
			}
		}
	}

	protected virtual partial void PlatformStop()
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
		else if (mediaElement.Source is ResourceMediaSource)
		{
			string path = (mediaElement.Source as ResourceMediaSource)!.Path!;

			if (!string.IsNullOrWhiteSpace(path))
			{
				string directory = Path.GetDirectoryName(path) ?? "";
				string filename = Path.GetFileNameWithoutExtension(path);
				string extension = Path.GetExtension(path).Substring(1);
				var url = NSBundle.MainBundle.GetUrlForResource(filename,
					extension, directory);

				asset = AVAsset.FromUrl(url);
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
				var message = $"{player?.CurrentItem?.Error?.LocalizedDescription} - " +
				$"{player?.CurrentItem?.Error?.LocalizedFailureReason}";

				mediaElement.MediaFailed(
					new MediaFailedEventArgs(message));

				Logger?.LogError("{logMessage}", message);
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
		else if (playerItem is null)
		{
			mediaElement.CurrentStateChanged(MediaElementState.None);
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

	protected virtual partial void PlatformUpdateStatus()
	{
		if (player is null)
		{
			return;
		}

		if (playerItem is not null)
		{
			if (playerItem.Duration == CMTime.Indefinite)
			{
				var range = playerItem.SeekableTimeRanges?.LastOrDefault();

				if (range?.CMTimeRangeValue is not null)
				{
					mediaElement.Duration = ConvertTime(range.CMTimeRangeValue.Duration);
					mediaElement.Position = ConvertTime(playerItem.CurrentTime);
				}
			}
			else
			{
				mediaElement.Duration = ConvertTime(playerItem.Duration);
				mediaElement.Position = ConvertTime(playerItem.CurrentTime);
			}
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
		// no-op we loop through using the playedToEndObserver
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (player is not null)
			{
				player.Pause();
				DestroyErrorObservers();
				DestroyPlayedToEndObserver();

				rateObserver?.Dispose();
				currentItemErrorObserver?.Dispose();
				player.ReplaceCurrentItemWithPlayerItem(null);
				volumeObserver?.Dispose();
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

		volumeObserver = player.AddObserver("volume", NSKeyValueObservingOptions.New,
					VolumeChanged);
		statusObserver = player.AddObserver("status", valueObserverOptions, StatusChanged);
		timeControlStatusObserver = player.AddObserver("timeControlStatus",
			valueObserverOptions, TimeControlStatusChanged);
		rateObserver = AVPlayer.Notifications.ObserveRateDidChange(RateChanged);
	}

	void VolumeChanged(NSObservedChange e)
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		mediaElement.Volume = player.Volume;
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

		var newState = mediaElement.CurrentState;

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
		if (player is null || player.Status == AVPlayerStatus.Unknown
			|| player.CurrentItem?.Error is not null)
		{
			return;
		}

		var newState = mediaElement.CurrentState;

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
			Logger?.LogError("{logMessage}", message);
		}
		else
		{
			// Non-fatal error, just log
			message = args.Notification?.ToString() ??
				"Media playback failed for an unknown reason.";

			Logger?.LogWarning("{logMessage}", message);
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
				Logger?.LogWarning(e, "{logMessage}",
					$"Failed to play media to end.");
			}
		}
	}

	void RateChanged(object? sender, NSNotificationEventArgs args)
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		if (mediaElement.Speed != player.Rate)
		{
			mediaElement.Speed = player.Rate;
		}
	}
}
