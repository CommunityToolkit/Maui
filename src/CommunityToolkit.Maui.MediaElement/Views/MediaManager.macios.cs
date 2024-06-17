using AVFoundation;
using AVKit;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using CoreFoundation;
using CoreMedia;
using Foundation;
using MediaPlayer;
using Microsoft.Extensions.Logging;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MediaManager : IDisposable
{
	Metadata? metaData;

	// Media would still start playing when Speed was set although ShouldAutoPlay=False
	// This field was added to overcome that.
	bool isInitialSpeedSet;

	/// <summary>
	/// The default <see cref="NSKeyValueObservingOptions"/> flags used in the iOS and macOS observers.
	/// </summary>
	protected const NSKeyValueObservingOptions valueObserverOptions =
		NSKeyValueObservingOptions.Initial | NSKeyValueObservingOptions.New;

	/// <summary>
	/// Observer that tracks when an error has occurred in the playback of the current item.
	/// </summary>
	protected IDisposable? CurrentItemErrorObserver { get; set; }

	/// <summary>
	/// Observer that tracks when an error has occurred with media playback.
	/// </summary>
	protected NSObject? ErrorObserver { get; set; }

	/// <summary>
	/// Observer that tracks when the media has failed to play to the end.
	/// </summary>
	protected NSObject? ItemFailedToPlayToEndTimeObserver { get; set; }

	/// <summary>
	/// Observer that tracks when the playback of media has stalled.
	/// </summary>
	protected NSObject? PlaybackStalledObserver { get; set; }

	/// <summary>
	/// Observer that tracks when the media has played to the end.
	/// </summary>
	protected NSObject? PlayedToEndObserver { get; set; }

	/// <summary>
	/// The current media playback item.
	/// </summary>
	protected AVPlayerItem? PlayerItem { get; set; }

	/// <summary>
	/// The <see cref="AVPlayerViewController"/> that hosts the media Player.
	/// </summary>
	protected AVPlayerViewController? PlayerViewController { get; set; }

	/// <summary>
	/// Observer that tracks the playback rate of the media.
	/// </summary>
	protected IDisposable? RateObserver { get; set; }

	/// <summary>
	/// Observer that tracks the status of the media.
	/// </summary>
	protected IDisposable? StatusObserver { get; set; }

	/// <summary>
	/// Observer that tracks the time control status of the media.
	/// </summary>
	protected IDisposable? TimeControlStatusObserver { get; set; }

	/// <summary>
	/// Observer that tracks the volume of the media playback.
	/// </summary>
	protected IDisposable? VolumeObserver { get; set; }

	/// <summary>
	/// Observer that tracks if the audio is muted.
	/// </summary>
	protected IDisposable? MutedObserver { get; set; }

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaElement"/> on iOS and macOS.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaElement"/>.</returns>
	public (PlatformMediaElement Player, AVPlayerViewController PlayerViewController) CreatePlatformView()
	{
		Player = new();
		PlayerViewController = new()
		{
			Player = Player
		};
		// Pre-initialize Volume and Muted properties to the player object
		Player.Muted = MediaElement.ShouldMute;
		var volumeDiff = Math.Abs(Player.Volume - MediaElement.Volume);
		if (volumeDiff > 0.01)
		{
			Player.Volume = (float)MediaElement.Volume;
		}

		UIApplication.SharedApplication.BeginReceivingRemoteControlEvents();

#if IOS
		PlayerViewController.UpdatesNowPlayingInfoCenter = false;
#else
		PlayerViewController.UpdatesNowPlayingInfoCenter = true;
#endif
		var avSession = AVAudioSession.SharedInstance();
		avSession.SetCategory(AVAudioSessionCategory.Playback);
		avSession.SetActive(true);

		AddStatusObservers();
		AddPlayedToEndObserver();
		AddErrorObservers();

		return (Player, PlayerViewController);
	}

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MediaManager"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual partial void PlatformPlay()
	{
		if (Player?.CurrentTime == PlayerItem?.Duration)
		{
			return;
		}

		Player?.Play();
	}

	protected virtual partial void PlatformPause()
	{
		Player?.Pause();
	}

	protected virtual async partial Task PlatformSeek(TimeSpan position, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var seekTaskCompletionSource = new TaskCompletionSource();

		if (Player?.CurrentItem is null)
		{
			throw new InvalidOperationException($"{nameof(AVPlayer)}.{nameof(AVPlayer.CurrentItem)} is not yet initialized");
		}

		if (Player?.Status is not AVPlayerStatus.ReadyToPlay)
		{
			throw new InvalidOperationException($"{nameof(AVPlayer)}.{nameof(AVPlayer.Status)} must first be set to {AVPlayerStatus.ReadyToPlay}");
		}

		var ranges = Player.CurrentItem.SeekableTimeRanges;
		var seekToTime = new CMTime(Convert.ToInt64(position.TotalMilliseconds), 1000);

		foreach (var v in ranges)
		{
			if (seekToTime >= (seekToTime - v.CMTimeRangeValue.Start)
				&& seekToTime < (v.CMTimeRangeValue.Start + v.CMTimeRangeValue.Duration))
			{
				Player.Seek(seekToTime + v.CMTimeRangeValue.Start, complete =>
				{
					if (!complete)
					{
						throw new InvalidOperationException("Seek Failed");
					}

					seekTaskCompletionSource.SetResult();
				});
				break;
			}
		}

		await seekTaskCompletionSource.Task.WaitAsync(token);

		MediaElement.SeekCompleted();
	}

	protected virtual partial void PlatformStop()
	{
		// There's no Stop method so pause the video and reset its position
		Player?.Seek(CMTime.Zero);
		Player?.Pause();

		MediaElement.CurrentStateChanged(MediaElementState.Stopped);
	}

	protected virtual partial void PlatformUpdateAspect()
	{
		if (PlayerViewController is null)
		{
			return;
		}

		PlayerViewController.VideoGravity = MediaElement.Aspect switch
		{
			Aspect.Fill => AVLayerVideoGravity.Resize,
			Aspect.AspectFill => AVLayerVideoGravity.ResizeAspectFill,
			_ => AVLayerVideoGravity.ResizeAspect,
		};
	}

	protected virtual partial void PlatformUpdateSource()
	{
		MediaElement.CurrentStateChanged(MediaElementState.Opening);

		AVAsset? asset = null;
		if (Player is null)
		{
			return;
		}

		metaData ??= new(Player);
		metaData.ClearNowPlaying();

		if (MediaElement.Source is UriMediaSource uriMediaSource)
		{
			var uri = uriMediaSource.Uri;
			if (!string.IsNullOrWhiteSpace(uri?.AbsoluteUri))
			{
				asset = AVAsset.FromUrl(new NSUrl(uri.AbsoluteUri));
			}
		}
		else if (MediaElement.Source is FileMediaSource fileMediaSource)
		{
			var uri = fileMediaSource.Path;

			if (!string.IsNullOrWhiteSpace(uri))
			{
				asset = AVAsset.FromUrl(NSUrl.CreateFileUrl(uri));
			}
		}
		else if (MediaElement.Source is ResourceMediaSource resourceMediaSource)
		{
			var path = resourceMediaSource.Path;

			if (!string.IsNullOrWhiteSpace(path) && Path.HasExtension(path))
			{
				string directory = Path.GetDirectoryName(path) ?? "";
				string filename = Path.GetFileNameWithoutExtension(path);
				string extension = Path.GetExtension(path)[1..];
				var url = NSBundle.MainBundle.GetUrlForResource(filename,
					extension, directory);

				asset = AVAsset.FromUrl(url);
			}
			else
			{
				Logger.LogWarning("Invalid file path for ResourceMediaSource.");
			}
		}

		PlayerItem = asset is not null
			? new AVPlayerItem(asset)
			: null;

		metaData.SetMetadata(PlayerItem, MediaElement);
		CurrentItemErrorObserver?.Dispose();

		Player.ReplaceCurrentItemWithPlayerItem(PlayerItem);

		CurrentItemErrorObserver = PlayerItem?.AddObserver("error",
			valueObserverOptions, (NSObservedChange change) =>
			{
				if (Player.CurrentItem?.Error is null)
				{
					return;
				}

				var message = $"{Player.CurrentItem?.Error?.LocalizedDescription} - " +
					$"{Player.CurrentItem?.Error?.LocalizedFailureReason}";

				MediaElement.MediaFailed(
					new MediaFailedEventArgs(message));

				Logger.LogError("{logMessage}", message);
			});

		if (PlayerItem is not null && PlayerItem.Error is null)
		{
			MediaElement.MediaOpened();

			if (MediaElement.ShouldAutoPlay)
			{
				Player.Play();
			}
		}
		else if (PlayerItem is null)
		{
			MediaElement.CurrentStateChanged(MediaElementState.None);
		}
	}

	protected virtual partial void PlatformUpdateSpeed()
	{
		if (PlayerViewController?.Player is null)
		{
			return;
		}

		// First time we're getting a playback speed and should NOT auto play, do nothing.
		if (!isInitialSpeedSet && !MediaElement.ShouldAutoPlay)
		{
			isInitialSpeedSet = true;
			return;
		}

		PlayerViewController.Player.Rate = (float)MediaElement.Speed;
	}

	protected virtual partial void PlatformUpdateShouldShowPlaybackControls()
	{
		if (PlayerViewController is null)
		{
			return;
		}

		PlayerViewController.ShowsPlaybackControls =
			MediaElement.ShouldShowPlaybackControls;
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (Player is null)
		{
			return;
		}

		if (PlayerItem is not null)
		{
			if (PlayerItem.Duration == CMTime.Indefinite)
			{
				var range = PlayerItem.SeekableTimeRanges?.LastOrDefault();

				if (range?.CMTimeRangeValue is not null)
				{
					MediaElement.Duration = ConvertTime(range.CMTimeRangeValue.Duration);
					MediaElement.Position = ConvertTime(PlayerItem.CurrentTime);
				}
			}
			else
			{
				MediaElement.Duration = ConvertTime(PlayerItem.Duration);
				MediaElement.Position = ConvertTime(PlayerItem.CurrentTime);
			}
		}
		else
		{
			Player.Pause();
			MediaElement.Duration = MediaElement.Position = TimeSpan.Zero;
		}
	}

	protected virtual partial void PlatformUpdateVolume()
	{
		if (Player is null)
		{
			return;
		}

		var volumeDiff = Math.Abs(Player.Volume - MediaElement.Volume);
		if (volumeDiff > 0.01)
		{
			Player.Volume = (float)MediaElement.Volume;
		}
	}

	protected virtual partial void PlatformUpdateShouldKeepScreenOn()
	{
		if (Player is null)
		{
			return;
		}

		Player.PreventsDisplaySleepDuringVideoPlayback = MediaElement.ShouldKeepScreenOn;
	}

	protected virtual partial void PlatformUpdateShouldMute()
	{
		if (Player is null)
		{
			return;
		}

		Player.Muted = MediaElement.ShouldMute;
	}

	protected virtual partial void PlatformUpdateShouldLoopPlayback()
	{
		// no-op we loop through using the PlayedToEndObserver
	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="MediaManager"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (Player is not null)
			{
				Player.Pause();
				Player.InvokeOnMainThread(() =>
				{
					UIApplication.SharedApplication.EndReceivingRemoteControlEvents();
				});

				var audioSession = AVAudioSession.SharedInstance();
				audioSession.SetActive(false);

				DestroyErrorObservers();
				DestroyPlayedToEndObserver();

				RateObserver?.Dispose();
				RateObserver = null;

				CurrentItemErrorObserver?.Dispose();
				CurrentItemErrorObserver = null;

				Player.ReplaceCurrentItemWithPlayerItem(null);

				MutedObserver?.Dispose();
				MutedObserver = null;

				VolumeObserver?.Dispose();
				VolumeObserver = null;

				StatusObserver?.Dispose();
				StatusObserver = null;

				TimeControlStatusObserver?.Dispose();
				TimeControlStatusObserver = null;

				Player.Dispose();
				Player = null;
			}

			PlayerViewController?.Dispose();
			PlayerViewController = null;
		}
	}

	static TimeSpan ConvertTime(CMTime cmTime) => TimeSpan.FromSeconds(double.IsNaN(cmTime.Seconds) ? 0 : cmTime.Seconds);

	void AddStatusObservers()
	{
		if (Player is null)
		{
			return;
		}

		MutedObserver = Player.AddObserver("muted", valueObserverOptions, MutedChanged);
		VolumeObserver = Player.AddObserver("volume", valueObserverOptions, VolumeChanged);
		StatusObserver = Player.AddObserver("status", valueObserverOptions, StatusChanged);
		TimeControlStatusObserver = Player.AddObserver("timeControlStatus", valueObserverOptions, TimeControlStatusChanged);
		RateObserver = AVPlayer.Notifications.ObserveRateDidChange(RateChanged);
	}

	void VolumeChanged(NSObservedChange e)
	{
		if (Player is null)
		{
			return;
		}

		var volumeDiff = Math.Abs(Player.Volume - MediaElement.Volume);
		if (volumeDiff > 0.01)
		{
			MediaElement.Volume = Player.Volume;
		}
	}

	void MutedChanged(NSObservedChange e)
	{
		if (Player is null)
		{
			return;
		}

		MediaElement.ShouldMute = Player.Muted;
	}

	void AddErrorObservers()
	{
		DestroyErrorObservers();

		ItemFailedToPlayToEndTimeObserver = AVPlayerItem.Notifications.ObserveItemFailedToPlayToEndTime(ErrorOccurred);
		PlaybackStalledObserver = AVPlayerItem.Notifications.ObservePlaybackStalled(ErrorOccurred);
		ErrorObserver = AVPlayerItem.Notifications.ObserveNewErrorLogEntry(ErrorOccurred);
	}

	void AddPlayedToEndObserver()
	{
		DestroyPlayedToEndObserver();

		PlayedToEndObserver = AVPlayerItem.Notifications.ObserveDidPlayToEndTime(PlayedToEnd);
	}

	void DestroyErrorObservers()
	{
		ItemFailedToPlayToEndTimeObserver?.Dispose();
		PlaybackStalledObserver?.Dispose();
		ErrorObserver?.Dispose();
	}

	void DestroyPlayedToEndObserver()
	{
		PlayedToEndObserver?.Dispose();
	}

	void StatusChanged(NSObservedChange obj)
	{
		if (Player is null)
		{
			return;
		}

		var newState = Player.Status switch
		{
			AVPlayerStatus.Unknown => MediaElementState.Stopped,
			AVPlayerStatus.ReadyToPlay => MediaElementState.Paused,
			AVPlayerStatus.Failed => MediaElementState.Failed,
			_ => MediaElement.CurrentState
		};

		MediaElement.CurrentStateChanged(newState);
	}

	void TimeControlStatusChanged(NSObservedChange obj)
	{
		if (Player is null || Player.Status is AVPlayerStatus.Unknown
			|| Player.CurrentItem?.Error is not null)
		{
			return;
		}

		var newState = Player.TimeControlStatus switch
		{
			AVPlayerTimeControlStatus.Paused => MediaElementState.Paused,
			AVPlayerTimeControlStatus.Playing => MediaElementState.Playing,
			AVPlayerTimeControlStatus.WaitingToPlayAtSpecifiedRate => MediaElementState.Buffering,
			_ => MediaElement.CurrentState
		};

		metaData?.SetMetadata(PlayerItem, MediaElement);

		MediaElement.CurrentStateChanged(newState);
	}

	void ErrorOccurred(object? sender, NSNotificationEventArgs args)
	{
		string message;

		var error = Player?.CurrentItem?.Error;
		if (error is not null)
		{
			message = error.LocalizedDescription;

			MediaElement.MediaFailed(new MediaFailedEventArgs(message));
			Logger.LogError("{logMessage}", message);
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
		if (args.Notification.Object != PlayerViewController?.Player?.CurrentItem || Player is null)
		{
			return;
		}

		if (MediaElement.ShouldLoopPlayback)
		{
			PlayerViewController?.Player?.Seek(CMTime.Zero);
			Player.Play();
		}
		else
		{
			try
			{
				DispatchQueue.MainQueue.DispatchAsync(MediaElement.MediaEnded);
			}
			catch (Exception e)
			{
				Logger.LogWarning(e, "{logMessage}", $"Failed to play media to end.");
			}
		}
	}

	void RateChanged(object? sender, NSNotificationEventArgs args)
	{
		if (Player is null)
		{
			return;
		}

		if (!AreFloatingPointNumbersEqual(MediaElement.Speed, Player.Rate))
		{
			MediaElement.Speed = Player.Rate;
			if (metaData is not null)
			{
				metaData.NowPlayingInfo.PlaybackRate = (float)MediaElement.Speed;
				MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = metaData.NowPlayingInfo;
			}
		}
	}
}