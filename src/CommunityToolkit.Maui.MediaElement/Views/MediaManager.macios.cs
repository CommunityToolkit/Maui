using AVFoundation;
using AVKit;
using CommunityToolkit.Maui.Media.Services;
using CommunityToolkit.Maui.Views;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MediaManager : IDisposable
{
	const string playableKey = "playable";
	const string tracksKey = "tracks";

	Metadata? metaData;
	StreamAssetResourceLoader? streamResourceLoader;
	CancellationTokenSource? playerItemCancellationTokenSource;

	float? previousSpeed;

	bool hasMediaOpened;

	/// <summary>
	/// The default <see cref="NSKeyValueObservingOptions"/> flags used in the iOS and macOS observers.
	/// </summary>
	protected NSKeyValueObservingOptions ValueObserverOptions => NSKeyValueObservingOptions.Initial | NSKeyValueObservingOptions.New;

	/// <summary>
	/// Observer that tracks the status of the current item, both when it becomes ready to play and when it fails.
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
		PlayerViewController.ShowsPlaybackControls = MediaElement.ShouldShowPlaybackControls;

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
		// Media that played to its end has nothing left to play: Play() starts it again from its start, as on Android.
		if (Player is not null && PlayerItem is not null && Player.CurrentTime == PlayerItem.Duration)
		{
			Player.Seek(CMTime.Zero);
		}

		PlayAtSpeed();
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

		// AVPlayer moves the playhead to a nearby key frame, which for the end of the media is one before the end: the end is sought without a
		// tolerance before it, so SeekTo(Duration) goes to the end.
		var (seekToTime, isEnd) = ClampToSeekableTimeRanges(Player.CurrentItem, position);
		Player.Seek(seekToTime, isEnd ? CMTime.Zero : CMTime.PositiveInfinity, CMTime.PositiveInfinity, _ =>
		{
			// AVPlayer reports a seek that something else interrupted (Stop(), another seek, the scrubber of the playback controls, a remote
			// command or the restart of a looping media) as not finished. The seek has ended all the same: like on Android, SeekTo completes and
			// raises SeekCompleted
			seekTaskCompletionSource.TrySetResult();
		});

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

	protected virtual partial ValueTask PlatformUpdateSource()
	{
		MediaElement.CurrentStateChanged(MediaElementState.Opening);

		if (Player is null)
		{
			return ValueTask.CompletedTask;
		}

		// Clean up previous stream resource loader if switching sources
		if (MediaElement.Source is not StreamMediaSource)
		{
			streamResourceLoader?.Dispose();
			streamResourceLoader = null;
		}

		// Metadata is retained for the app's lifetime by the shared MPRemoteCommandCenter, so pass a weak-reference
		// playback callback. Passing PlayAtSpeed directly would give Metadata a strong reference to this MediaManager and,
		// through it, its MediaElement and MauiContext, keeping them (and their managed view trees) alive after disposal.
		metaData ??= new(Player, CreatePlaybackCallback(new WeakReference<MediaManager>(this)));

		static Action CreatePlaybackCallback(WeakReference<MediaManager> manager)
		{
			return () =>
			{
				if (manager.TryGetTarget(out var target))
				{
					target.PlayAtSpeed();
				}
			};
		}
		Metadata.ClearNowPlaying();
		PlayerViewController?.ContentOverlayView?.Subviews.FirstOrDefault()?.RemoveFromSuperview();

		// Only a failure to create the asset or the item of the source is a failure of the source (below: MediaFailed). The events that the update
		// raises (StateChanged, also from the observers of the player while its item is replaced) are not in this part: an exception of their
		// handlers is not a failure of the source, and propagates to the code that set the Source
		Exception? sourceException = null;
		try
		{
			PlayerItem = CreateAsset() is AVAsset asset ? new AVPlayerItem(asset) : null;
		}
		catch (Exception exception)
		{
			// The source cannot be applied: it fails (below) instead of the previous media staying in the player
			sourceException = exception;
			PlayerItem = null;
		}

		metaData.SetMetadata(PlayerItem, MediaElement);
		DestroyPlayerItemStatusObserver();

		hasMediaOpened = false;

		var playerItem = PlayerItem;
		Player.ReplaceCurrentItemWithPlayerItem(playerItem);

		// A StateChanged handler raised during the `ReplaceCurrentItemWithPlayerItem` can change the source or disconnect the handler
		if (Player is null || !ReferenceEquals(PlayerItem, playerItem))
		{
			return ValueTask.CompletedTask;
		}

		if (playerItem is null)
		{
			if (sourceException is not null)
			{
				ReportMediaFailure(sourceException.Message, sourceException);
			}
			else
			{
				MediaElement.MediaWidth = MediaElement.MediaHeight = 0;

				MediaElement.CurrentStateChanged(MediaElementState.None);
			}
		}
		else
		{
			// Bind the callback to the observed item and its token, which is canceled when the source changes or the player is disposed
			playerItemCancellationTokenSource = new();
			var token = playerItemCancellationTokenSource.Token;

			var observer = playerItem.AddObserver("status", ValueObserverOptions,
				_ => DispatchQueue.MainQueue.DispatchAsync(() => PlayerItemStatusChanged(playerItem, token).SafeFireAndForget(OnPlayerItemStatusChangedException, continueOnCapturedContext: true)));

			// The initial notification runs before `AddObserver` returns, and an event handler it raises can change the source or disconnect the handler
			if (Player is null || !ReferenceEquals(PlayerItem, playerItem))
			{
				observer.Dispose();
				return ValueTask.CompletedTask;
			}

			CurrentItemErrorObserver = observer;
		}

		return ValueTask.CompletedTask;
	}

	protected virtual partial void PlatformUpdateSpeed()
	{
		if (PlayerViewController?.Player is not AVPlayer player)
		{
			return;
		}

		var speed = (float)MediaElement.Speed;
		var previous = previousSpeed;
		previousSpeed = speed;

		// The playback controls of the AVPlayerViewController call AVPlayer.Play(), which starts at the default rate, so the default rate follows
		// the Speed (setting it does not start playback). Before iOS 16 and Mac Catalyst 16 there is no default rate: the playback controls start
		// playback at a rate of 1, which RateChanged reports as the Speed.
		if (speed > 0 && (OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16)))
		{
			player.DefaultRate = speed;
		}

		// RateChanged copies the player's rate into Speed, so don't send it back: setting the rate again starts a new rate change, and when
		// two rates are set in the same main-loop turn AVPlayer reports the earlier one again afterwards, so the two would keep replacing each other.
		// Skip only when the requested rate (as the same float AVPlayer last stored, Rate was set to (float)Speed) matches the native rate
		// within a 0.005 rounding margin to handle floating point math.
		if (Math.Abs(speed - player.Rate) < .005)
		{
			return;
		}

		// Setting the rate starts playback: it changes while the media plays (a Speed of 0 pauses it), and a positive Speed that
		// follows a Speed of 0 resumes playback, as on Android and Windows; any other change waits for the next start of playback.
		if (player.Rate is not 0)
		{
			player.Rate = speed;
		}
		else if (previous is 0 && speed > 0)
		{
			PlayAtSpeed();
		}
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
				var range = PlayerItem.SeekableTimeRanges.LastOrDefault();

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
			// Disposed before Pause(), which raises the time control status observer even for a player that is already paused: it would publish this
			// player's Now Playing info again, and Metadata.Dispose would then clear it while another MediaElement's media is shown
			metaData?.Dispose();
			metaData = null;

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

				DestroyPlayerItemStatusObserver();

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

			// An event handler raised above (for example a StateChanged handler of the Pause()) can have set a source, whose update created a
			// new Metadata
			metaData?.Dispose();
			metaData = null;

			PlayerViewController?.Dispose();
			PlayerViewController = null;
		}
	}

	AVAsset? CreateAsset()
	{
		AVAsset? asset = null;
		if (MediaElement.Source is UriMediaSource uriMediaSource)
		{
			var uri = uriMediaSource.Uri;
			if (!string.IsNullOrWhiteSpace(uri?.AbsoluteUri))
			{
				var nsUrl = new NSUrl(uri.AbsoluteUri);
				var headers = uriMediaSource.HttpHeaders;
				if (headers.Count > 0)
				{
					var pairs = headers.ToArray();
					var nativeHeaders = NSDictionary.FromObjectsAndKeys(
						pairs.Select(p => p.Value).ToArray<object>(),
						pairs.Select(p => p.Key).ToArray<object>());
					var options = new NSDictionary("AVURLAssetHTTPHeaderFieldsKey", nativeHeaders);
					asset = new AVUrlAsset(nsUrl, new AVUrlAssetOptions(options));
				}
				else
				{
					asset = AVAsset.FromUrl(nsUrl);
				}
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

				if (url is not null)
				{
					asset = AVAsset.FromUrl(url);
				}
				else
				{
					Logger.LogWarning("Resource file was not found.");
				}
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

				// A stream that cannot be read throws here: the new source fails instead of the previous media staying in the player.
				streamResourceLoader?.Dispose();
				streamResourceLoader = null;
				streamResourceLoader = new StreamAssetResourceLoader(streamMediaSource.Stream, GetStreamContentType(streamMediaSource.Stream));

				// Assign the resource loader delegate to a serial background queue to avoid blocking the UI thread
				var resourceLoaderQueue = new DispatchQueue("CommunityToolkit.Maui.MediaElement.StreamResourceLoader");
				urlAsset.ResourceLoader.SetDelegate(streamResourceLoader, resourceLoaderQueue);

				asset = urlAsset;
			}
		}
		else if (MediaElement.Source is not null)
		{
			throw new NotSupportedException($"{MediaElement.Source.GetType().FullName} is not yet supported for {nameof(MediaElement.Source)}");
		}

		return asset;
	}

	static string GetStreamContentType(Stream stream)
	{
		// The file type header is 12 bytes long.
		if (stream is not { CanSeek: true, Length: >= 12 })
		{
			return StreamAssetResourceLoader.DefaultContentType;
		}

		var originalPosition = stream.Position;
		try
		{
			stream.Position = 0;
			var buffer = new byte[12];

			// Stream.Read may return fewer bytes than requested before the end of the stream
			var bytesRead = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);

			if (bytesRead >= 12)
			{
				// Check for MP4/M4V/MOV signature (ftyp box at offset 4)
				if (buffer[4] == 0x66 && buffer[5] == 0x74 && buffer[6] == 0x79 && buffer[7] == 0x70)
				{
					// Check specific brand
					var brand = System.Text.Encoding.ASCII.GetString(buffer, 8, 4);

					// Most iOS videos will be either mp4, m4v, or qt (QuickTime)
					// For AVFoundation, we should use UTI: "public.mpeg-4" or "com.apple.quicktime-movie"
					return brand.StartsWith("qt", StringComparison.Ordinal)
						? "com.apple.quicktime-movie"
						: StreamAssetResourceLoader.DefaultContentType;
				}
			}
		}
		finally
		{
			stream.Position = originalPosition;
		}

		// Default to MPEG-4 (MP4) - covers most cases
		// Using UTI format for iOS/macOS
		return "public.mpeg-4";
	}

	static TimeSpan ConvertTime(CMTime cmTime) => TimeSpan.FromSeconds(double.IsNaN(cmTime.Seconds) ? 0 : cmTime.Seconds);

	// The seekable time ranges span the media, or the window of a live stream. Without any, only a position before the start of the media can
	// be clamped, to the start.
	static (CMTime Time, bool IsEnd) ClampToSeekableTimeRanges(AVPlayerItem item, TimeSpan position)
	{
		var time = new CMTime(Convert.ToInt64(position.TotalMilliseconds), 1000);
		var ranges = item.SeekableTimeRanges.Select(static value => value.CMTimeRangeValue).ToArray();
		if (ranges.Length is 0)
		{
			return (time < CMTime.Zero ? CMTime.Zero : time, false);
		}

		var start = ranges[0].Start;
		var end = ranges[0].Start + ranges[0].Duration;
		foreach (var range in ranges.Skip(1))
		{
			var rangeEnd = range.Start + range.Duration;
			start = range.Start < start ? range.Start : start;
			end = rangeEnd > end ? rangeEnd : end;
		}

		// The Duration is the end as a TimeSpan (ConvertTime), which the whole milliseconds of time can round down to before the end (a media of
		// 61061/30000 s, 2035.3667 ms, is sought as 2035 ms): a position at or after the end as a TimeSpan is the end too
		var isEnd = time >= end || (end.IsNumeric && position >= ConvertTime(end));

		return time < start ? (start, false) : isEnd ? (end, true) : (time, false);
	}

	static (int Width, int Height) GetVideoDimensions(AVPlayerItem avPlayerItem, AVAssetTrack? videoTrack)
	{
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

	static async Task<AVAssetTrack?> GetTrack(AVAsset asset, CancellationToken token)
	{
		if (!(OperatingSystem.IsMacCatalystVersionAtLeast(18)
			  || OperatingSystem.IsIOSVersionAtLeast(18)))
		{
			// AVAsset.TracksWithMediaType is Obsolete on iOS 18+ and MacCatalyst 18+
			return asset.TracksWithMediaType(AVMediaTypes.Video.GetConstant() ?? "0").FirstOrDefault();
		}

		var tracks = await asset.LoadTracksWithMediaTypeAsync(AVMediaTypes.Video.GetConstant() ?? "0").WaitAsync(token);

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

	void SetPoster(in AVPlayerItem playerItem)
	{
		if (metaData is null)
		{
			return;
		}

		if (playerItem.Asset.Tracks.Length == 0)
		{
			// No video track found and no tracks found. This is likely an audio file. So we can't set a poster.
			return;
		}

		if (PlayerViewController?.View is not null && PlayerViewController.ContentOverlayView is not null && !string.IsNullOrEmpty(MediaElement.MetadataArtworkUrl))
		{
			// Artwork that cannot be read or decoded shows no poster (an image without a size has no aspect ratio to keep)
			var artworkData = NSUrl.FromString(MediaElement.MetadataArtworkUrl) is NSUrl artworkUrl ? NSData.FromUrl(artworkUrl) : null;
			var image = artworkData is null ? null : UIImage.LoadFromData(artworkData);
			if (image is null || image.Size.Width <= 0 || image.Size.Height <= 0)
			{
				return;
			}

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

	void DestroyPlayerItemStatusObserver()
	{
		CurrentItemErrorObserver?.Dispose();
		CurrentItemErrorObserver = null;

		playerItemCancellationTokenSource?.Cancel();
		playerItemCancellationTokenSource?.Dispose();
		playerItemCancellationTokenSource = null;
	}


	async ValueTask PlayerItemStatusChanged(AVPlayerItem playerItem, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		switch (playerItem.Status)
		{
			case AVPlayerItemStatus.ReadyToPlay:
				if (hasMediaOpened)
				{
					return;
				}

				hasMediaOpened = true;

				// The video track is only used for the dimensions and the poster, so failing to load it must not prevent MediaOpened
				AVAssetTrack? videoTrack = null;
				var didLoadTracks = false;
				try
				{
					videoTrack = await GetTrack(playerItem.Asset, token);
					didLoadTracks = true;
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					Logger.LogWarning(ex, "{LogMessage}", "Failed to load the video track; using the presentation size instead.");
				}

				token.ThrowIfCancellationRequested();

				// The item can fail while its tracks are loading
				if (Player is null || playerItem.Status is not AVPlayerItemStatus.ReadyToPlay)
				{
					return;
				}

				// Event handlers raised below run synchronously and may change the source or disconnect the handler, which cancels the token
				(MediaElement.MediaWidth, MediaElement.MediaHeight) = GetVideoDimensions(playerItem, videoTrack);

				// Handles CMTime.Indefinite durations (e.g. HLS/live) via the seekable range
				PlatformUpdatePosition();
				token.ThrowIfCancellationRequested();

				// Start playback before raising MediaOpened so a Pause(), Stop() or SeekTo() from a MediaOpened handler isn't overridden. A Speed
				// of 0 pauses the media (PlatformUpdateSpeed): Play() would start it at the default rate, which RateChanged would make the Speed
				if (MediaElement.ShouldAutoPlay && MediaElement.Speed is not 0)
				{
					// Start at the requested Speed with a single rate change. Play() followed by setting Rate changes the rate twice,
					// and RateChanged, which copies the player's rate into Speed, then keeps switching Speed between 1 and the requested value
					PlayAtSpeed();
					token.ThrowIfCancellationRequested();
				}

				MediaElement.CurrentStateChanged(Player.TimeControlStatus switch
				{
					AVPlayerTimeControlStatus.Playing => MediaElementState.Playing,
					AVPlayerTimeControlStatus.WaitingToPlayAtSpecifiedRate => MediaElementState.Buffering,
					_ => MediaElementState.Paused
				});
				token.ThrowIfCancellationRequested();

				MediaElement.MediaOpened();
				token.ThrowIfCancellationRequested();

				if (didLoadTracks && videoTrack is null)
				{
					SetPoster(playerItem);
				}

				break;

			case AVPlayerItemStatus.Failed:
				var error = playerItem.Error;
				var message = error is not null
					? $"{error.LocalizedDescription} - {error.LocalizedFailureReason}"
					: "AVPlayerItem failed.";

				ReportMediaFailure(message);
				break;

			case AVPlayerItemStatus.Unknown:
				// AVFoundation never makes the item of media without any track (AVAsset.Playable is false) ReadyToPlay or Failed, so such media
				// would be neither opened nor failed. The tracks of media that cannot be read fail to load instead, and its item fails.
				var asset = playerItem.Asset;
				await asset.LoadValuesTaskAsync([playableKey, tracksKey]).WaitAsync(token);
				token.ThrowIfCancellationRequested();

				// Playable is read first: reading Tracks creates the managed wrappers of the tracks, which only an asset that cannot play may get
				// here (the video track of playable media is loaded with LoadTracksWithMediaTypeAsync once it is ready)
				if (Player is not null
					&& playerItem.Status is AVPlayerItemStatus.Unknown
					&& asset.StatusOfValue(playableKey, out _) is AVKeyValueStatus.Loaded
					&& asset.StatusOfValue(tracksKey, out _) is AVKeyValueStatus.Loaded
					&& !asset.Playable
					&& asset.Tracks.Length is 0)
				{
					ReportMediaFailure("The media has no tracks to play.");
				}

				break;
		}
	}

	void ReportMediaFailure(string message, Exception? exception = null)
	{
		// Only media that opens gets a size: a failed source must not keep the size of the previous media
		MediaElement.MediaWidth = MediaElement.MediaHeight = 0;

		MediaElement.MediaFailed(new MediaFailedEventArgs(message));
		Logger.LogError(exception, "{LogMessage}", message);
	}

	void OnPlayerItemStatusChangedException(Exception exception)
	{
		// Cancellation means the source changed or the player was disposed, which is expected
		if (exception is OperationCanceledException)
		{
			return;
		}

		Logger.LogError(exception, "{LogMessage}", "Failed to handle AVPlayerItem status change.");
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

			// The player can become ready after its item was removed (a cleared source) or while its item cannot be played: only media
			// that is ready to play is paused
			AVPlayerStatus.ReadyToPlay when PlayerItem?.Status is AVPlayerItemStatus.ReadyToPlay => MediaElementState.Paused,
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

		// The observers receive the notifications of every AVPlayerItem in the app: those of another player's item are not this media's
		if (args.Notification?.Object is AVPlayerItem notifiedItem && !notifiedItem.Equals(Player?.CurrentItem))
		{
			return;
		}

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

			Logger.LogWarning("{LogMessage}", message);
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
			PlayAtSpeed();
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
		// The observer receives the rate changes of every AVPlayer in the app
		if (Player is not AVPlayer player || !ReferenceEquals(args.Notification?.Object, player))
		{
			return;
		}

		// The notification is disposed once this handler returns (NSNotificationCenter.AddObserver), so the dispatched update only uses the player
		if (Dispatcher.IsDispatchRequired)
		{
			Dispatcher.Dispatch(() => UpdateSpeedFromRate(player));
			return;
		}

		UpdateSpeedFromRate(player);
	}

	void UpdateSpeedFromRate(AVPlayer player)
	{
		// A dispatched update can run after the player was disposed. AVPlayer reports a rate of 0 while paused, that is not a change of the requested Speed
		if (!ReferenceEquals(player, Player) || player.Rate is 0)
		{
			return;
		}

		if (!AreFloatingPointNumbersEqual(MediaElement.Speed, player.Rate))
		{
			MediaElement.Speed = player.Rate;
		}

		if (metaData is not null)
		{
			metaData.NowPlayingInfo.PlaybackRate = (float)MediaElement.Speed;
			metaData.PublishNowPlayingInfo();
		}
	}

	void PlayAtSpeed()
	{
		if (Player is null)
		{
			return;
		}

		// AVPlayer.Play() starts playback at its default rate (1 unless PlatformUpdateSpeed set it), setting the rate starts playback at the requested Speed
		if (MediaElement.Speed is 0)
		{
			Player.Play();
		}
		else
		{
			Player.Rate = (float)MediaElement.Speed;
		}
	}
}