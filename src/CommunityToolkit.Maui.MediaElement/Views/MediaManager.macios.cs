using AVFoundation;
using AVKit;
using CommunityToolkit.Maui.Views;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using Foundation;
using MediaPlayer;
using Microsoft.Extensions.Logging;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MediaManager : IDisposable
{
	Metadata? metaData;
	StreamAssetResourceLoader? streamResourceLoader;

	// Media would still start playing when Speed was set although ShouldAutoPlay=False
	// This field was added to overcome that.
	bool isInitialSpeedSet;

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

	/// <summary>
	/// The default <see cref="NSKeyValueObservingOptions"/> flags used in the iOS and macOS observers.
	/// </summary>
	protected NSKeyValueObservingOptions ValueObserverOptions => NSKeyValueObservingOptions.Initial | NSKeyValueObservingOptions.New;

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

		if (Player.Status is not AVPlayerStatus.ReadyToPlay)
		{
			throw new InvalidOperationException($"{nameof(AVPlayer)}.{nameof(AVPlayer.Status)} must first be set to {AVPlayerStatus.ReadyToPlay}");
		}

		var ranges = Player.CurrentItem.SeekableTimeRanges;
		var seekToTime = new CMTime(Convert.ToInt64(position.TotalMilliseconds), 1000);
		foreach (var range in ranges.Select(r => r.CMTimeRangeValue))
		{
			if (seekToTime >= range.Start && seekToTime < (range.Start + range.Duration))
			{
				Player.Seek(seekToTime, complete =>
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

	protected virtual async partial ValueTask PlatformUpdateSource()
	{
		MediaElement.CurrentStateChanged(MediaElementState.Opening);

		AVAsset? asset = null;
		if (Player is null)
		{
			return;
		}

		// Clean up previous stream resource loader if switching sources
		if (MediaElement.Source is not StreamMediaSource)
		{
			streamResourceLoader?.Dispose();
			streamResourceLoader = null;
		}

		metaData ??= new(Player);
		Metadata.ClearNowPlaying();
		PlayerViewController?.ContentOverlayView?.Subviews.FirstOrDefault()?.RemoveFromSuperview();

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
		else if (MediaElement.Source is StreamMediaSource streamMediaSource)
		{
			if (streamMediaSource.Stream is not null)
			{
				// Create a custom URL scheme for the stream
				var streamUrl = new NSUrl("stream://media");

				// Create an AVURLAsset with the custom scheme
				var urlAsset = new AVUrlAsset(streamUrl);

				// Create and set up the resource loader
				streamResourceLoader?.Dispose();
				streamResourceLoader = new StreamAssetResourceLoader(streamMediaSource.Stream, GetStreamContentType(streamMediaSource.Stream));

				// Assign the resource loader delegate
				urlAsset.ResourceLoader.SetDelegate(streamResourceLoader, DispatchQueue.MainQueue);

				asset = urlAsset;
			}
		}

		PlayerItem = asset is not null
			? new AVPlayerItem(asset)
			: null;

		metaData.SetMetadata(PlayerItem, MediaElement);
		CurrentItemErrorObserver?.Dispose();

		Player.ReplaceCurrentItemWithPlayerItem(PlayerItem);

		CurrentItemErrorObserver = PlayerItem?.AddObserver("error",
			ValueObserverOptions, (NSObservedChange change) =>
			{
				if (Player.CurrentItem?.Error is null)
				{
					return;
				}

				var message = $"{Player.CurrentItem?.Error?.LocalizedDescription} - " +
							  $"{Player.CurrentItem?.Error?.LocalizedFailureReason}";

				MediaElement.MediaFailed(
					new MediaFailedEventArgs(message));

				Logger.LogError("{LogMessage}", message);
			});

		if (PlayerItem is not null && PlayerItem.Error is null)
		{
			MediaElement.MediaOpened();

			(MediaElement.MediaWidth, MediaElement.MediaHeight) = await GetVideoDimensions(PlayerItem);

			if (MediaElement.ShouldAutoPlay)
			{
				Player.Play();
			}

			await SetPoster();
		}
		else if (PlayerItem is null)
		{
			MediaElement.MediaWidth = MediaElement.MediaHeight = 0;

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

		UIApplication.SharedApplication.IdleTimerDisabled = MediaElement.ShouldKeepScreenOn;
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
				Player.InvokeOnMainThread(() => { UIApplication.SharedApplication.EndReceivingRemoteControlEvents(); });
				// disable the idle timer so screen turns off when media is not playing
				UIApplication.SharedApplication.IdleTimerDisabled = false;
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

			streamResourceLoader?.Dispose();
			streamResourceLoader = null;

			PlayerViewController?.Dispose();
			PlayerViewController = null;
		}
	}

	static string GetStreamContentType(Stream stream)
	{
		// Try to detect content type from magic bytes
		if (stream.CanSeek && stream.Length > 12)
		{
			var originalPosition = stream.Position;
			try
			{
				stream.Position = 0;
				var buffer = new byte[12];
				var bytesRead = stream.Read(buffer, 0, 12);

				if (bytesRead >= 12)
				{
					// Check for MP4/M4V/MOV signature (ftyp box at offset 4)
					if (buffer[4] == 0x66 && buffer[5] == 0x74 && buffer[6] == 0x79 && buffer[7] == 0x70)
					{
						// Check specific brand
						var brand = System.Text.Encoding.ASCII.GetString(buffer, 8, 4);

						// Most iOS videos will be either mp4, m4v, or qt (QuickTime)
						// For AVFoundation, we should use UTI: "public.mpeg-4" or "com.apple.quicktime-movie"
						if (brand.StartsWith("qt", StringComparison.Ordinal))
						{
							return "com.apple.quicktime-movie";
						}

						return "public.mpeg-4";
					}
				}
			}
			finally
			{
				stream.Position = originalPosition;
			}
		}

		// Default to MPEG-4 (MP4) - covers most cases
		// Using UTI format for iOS/macOS
		return "public.mpeg-4";
	}

	static TimeSpan ConvertTime(CMTime cmTime) => TimeSpan.FromSeconds(double.IsNaN(cmTime.Seconds) ? 0 : cmTime.Seconds);

	static async Task<(int Width, int Height)> GetVideoDimensions(AVPlayerItem avPlayerItem)
	{
		// Create an AVAsset instance with the video file URL
		var asset = avPlayerItem.Asset;

		// Retrieve the video track
		var videoTrack = await GetTrack(asset);

		if (videoTrack is null)
		{
			// HLS doesn't have tracks, try to get the dimensions this way
			return !avPlayerItem.PresentationSize.IsEmpty
				? ((int)avPlayerItem.PresentationSize.Width, (int)avPlayerItem.PresentationSize.Height)
				// If all else fails, just return 0, 0
				: (0, 0);
		}

		// Get the natural size of the video
		var size = videoTrack.NaturalSize;
		var preferredTransform = videoTrack.PreferredTransform;

		// Apply the preferred transform to get the correct dimensions
		var transformedSize = CGAffineTransform.CGRectApplyAffineTransform(new CGRect(CGPoint.Empty, size), preferredTransform);
		var width = Math.Abs(transformedSize.Width);
		var height = Math.Abs(transformedSize.Height);

		return ((int)width, (int)height);
	}

	static async Task<AVAssetTrack?> GetTrack(AVAsset asset)
	{
		if (!(OperatingSystem.IsMacCatalystVersionAtLeast(18)
			  || OperatingSystem.IsIOSVersionAtLeast(18)))
		{
			// AVAsset.TracksWithMediaType is Obsolete on iOS 18+ and MacCatalyst 18+
			return asset.TracksWithMediaType(AVMediaTypes.Video.GetConstant() ?? "0").FirstOrDefault();
		}

		var tracks = await asset.LoadTracksWithMediaTypeAsync(AVMediaTypes.Video.GetConstant() ?? "0");

		return tracks.Count <= 0 ? null : tracks[0];
	}

	void AddStatusObservers()
	{
		if (Player is null)
		{
			return;
		}

		MutedObserver = Player.AddObserver("muted", ValueObserverOptions, MutedChanged);
		VolumeObserver = Player.AddObserver("volume", ValueObserverOptions, VolumeChanged);
		StatusObserver = Player.AddObserver("status", ValueObserverOptions, StatusChanged);
		TimeControlStatusObserver = Player.AddObserver("timeControlStatus", ValueObserverOptions, TimeControlStatusChanged);
		RateObserver = AVPlayer.Notifications.ObserveRateDidChange(RateChanged);
	}

	async Task SetPoster()
	{
		if (PlayerItem is null || metaData is null)
		{
			return;
		}

		var videoTrack = await GetTrack(PlayerItem.Asset);
		if (videoTrack is not null)
		{
			return;
		}

		if (PlayerItem.Asset.Tracks.Length == 0)
		{
			// No video track found and no tracks found. This is likely an audio file. So we can't set a poster.
			return;
		}

		if (PlayerViewController?.View is not null && PlayerViewController.ContentOverlayView is not null && !string.IsNullOrEmpty(MediaElement.MetadataArtworkUrl))
		{
			var image = UIImage.LoadFromData(NSData.FromUrl(new NSUrl(MediaElement.MetadataArtworkUrl))) ?? new UIImage();
			var imageView = new UIImageView(image)
			{
				ContentMode = UIViewContentMode.ScaleAspectFit,
				TranslatesAutoresizingMaskIntoConstraints = false,
				ClipsToBounds = true,
				AutoresizingMask = UIViewAutoresizing.FlexibleDimensions
			};

			PlayerViewController.ContentOverlayView.AddSubview(imageView);
			NSLayoutConstraint.ActivateConstraints(
			[
				imageView.CenterXAnchor.ConstraintEqualTo(PlayerViewController.ContentOverlayView.CenterXAnchor),
				imageView.CenterYAnchor.ConstraintEqualTo(PlayerViewController.ContentOverlayView.CenterYAnchor),
				imageView.WidthAnchor.ConstraintLessThanOrEqualTo(PlayerViewController.ContentOverlayView.WidthAnchor),
				imageView.HeightAnchor.ConstraintLessThanOrEqualTo(PlayerViewController.ContentOverlayView.HeightAnchor),

				// Maintain the aspect ratio
				imageView.WidthAnchor.ConstraintEqualTo(imageView.HeightAnchor, image.Size.Width / image.Size.Height)
			]);
		}
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
			Logger.LogError("{LogMessage}", message);
		}
		else
		{
			// Non-fatal error, just log
			message = args.Notification?.ToString() ??
					  "Media playback failed for an unknown reason.";

			Logger?.LogWarning("{LogMessage}", message);
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
				Logger.LogWarning(e, "{LogMessage}", $"Failed to play media to end.");
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
