using AVFoundation;
using AVKit;
using CoreFoundation;
using CoreMedia;
using Foundation;
using static CommunityToolkit.Maui.MediaElement.Helpers.ObserverExtensions;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaManager : IDisposable
{
	protected NSObject? playedToEndObserver;
	protected AVPlayerViewController? playerViewController;
	protected AVPlayerItem? playerItem;

	public (PlatformMediaView player, AVPlayerViewController playerViewController) CreatePlatformView()
	{
		player = new();
		playerViewController = new()
		{
			Player = player
		};

		AddPlayedToEndObserver();
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
		player?.Pause();
		player?.Seek(new CMTime(0, 1));
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

		player?.ReplaceCurrentItemWithPlayerItem(playerItem);
		if (playerItem is not null && mediaElement.AutoPlay)
		{
			player?.Play();
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

		playerViewController.ShowsPlaybackControls = mediaElement.ShowsPlaybackControls;
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
			player.Seek(CMTime.FromSeconds(mediaElement.Position.TotalSeconds, 1));
		}
	}

	protected virtual partial void PlatformUpdateStatus()
	{
		if (player is null)
		{
			return;
		}

		var videoStatus = MediaElementState.Closed;

		switch (player.Status)
		{
			case AVPlayerStatus.ReadyToPlay:
				switch (player.TimeControlStatus)
				{
					case AVPlayerTimeControlStatus.WaitingToPlayAtSpecifiedRate:
						videoStatus = MediaElementState.Buffering;
						break;

					case AVPlayerTimeControlStatus.Playing:
						videoStatus = MediaElementState.Playing;
						break;

					case AVPlayerTimeControlStatus.Paused:
						videoStatus = MediaElementState.Paused;
						break;
				}
				break;
		}

		// There is no real stopped state, if position is 0 and status is paused, assume stopped
		if (videoStatus == MediaElementState.Paused && mediaElement.Position == TimeSpan.Zero)
		{
			videoStatus = MediaElementState.Stopped;
		}

		mediaElement.CurrentState = videoStatus;

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


	protected virtual partial void PlatformUpdateIsLooping()
	{

	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (player is not null)
			{
				player.ReplaceCurrentItemWithPlayerItem(null);
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
		return TimeSpan.FromSeconds(double.IsNaN(cmTime.Seconds) ? 0 : cmTime.Seconds);
	}

	void AddPlayedToEndObserver()
	{
		DestroyPlayedToEndObserver();

		playedToEndObserver =
			NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, PlayedToEnd);
	}

	void DestroyPlayedToEndObserver()
	{
		if (playedToEndObserver is not null)
		{
			NSNotificationCenter.DefaultCenter.RemoveObserver(playedToEndObserver);
			DisposeObservers(ref playedToEndObserver);
		}
	}

	void PlayedToEnd(NSNotification notification)
	{
		if (notification.Object != playerViewController?.Player?.CurrentItem || player is null)
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
			// TODO Implement KeepScreenOn
			//SetKeepScreenOn(false);

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
