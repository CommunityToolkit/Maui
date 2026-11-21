using AVFoundation;
using CommunityToolkit.Maui.Views;
using CoreMedia;
using Foundation;
using MediaPlayer;
using Microsoft.Extensions.Logging;
using UIKit;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// What the image of an artwork is loaded from: a URL (with the <see cref="UriMediaSource.HttpHeaders"/> of its source), or the data read from a stream.
/// </summary>
sealed record ResolvedArtwork(NSUrl? Url, KeyValuePair<string, string>[] HttpHeaders, NSData? Data);

sealed class Metadata : IDisposable
{
	static readonly UIImage defaultUIImage = new();
	static readonly MPNowPlayingInfo nowPlayingInfoDefault = new()
	{
		AlbumTitle = string.Empty,
		Title = string.Empty,
		Artist = string.Empty,
		PlaybackDuration = 0,
		IsLiveStream = false,
		PlaybackRate = 0,
		ElapsedPlaybackTime = 0,
		Artwork = new(boundsSize: new(0, 0), requestHandler: _ => defaultUIImage)
	};

	static Metadata? nowPlayingInfoPublisher;

	readonly PlatformMediaElement player;
	readonly Action play;
	readonly ILogger? logger;
	readonly List<(MPRemoteCommand Command, NSObject Target)> remoteCommandTargets = [];

	// SetMetadata runs at every change of the playback status: a stream is read once, so that one that cannot seek still gives its artwork
	(Stream Stream, NSData? Data)? streamArtwork;

	/// <summary>
	/// Initializes a new instance of the <see cref="Metadata"/> class.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="play">Starts playback at the requested speed (<see cref="AVPlayer.Play"/> starts at <see cref="AVPlayer.DefaultRate"/>, which only exists from iOS 16 and Mac Catalyst 16).</param>
	/// <param name="logger">Logs artwork that cannot be loaded.</param>
	public Metadata(PlatformMediaElement player, Action play, ILogger? logger = null)
	{
		this.player = player;
		this.play = play;
		this.logger = logger;
		MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = nowPlayingInfoDefault;

		var commandCenter = MPRemoteCommandCenter.Shared;

		commandCenter.TogglePlayPauseCommand.Enabled = true;
		AddTarget(commandCenter.TogglePlayPauseCommand, ToggleCommand);

		commandCenter.PlayCommand.Enabled = true;
		AddTarget(commandCenter.PlayCommand, PlayCommand);

		commandCenter.PauseCommand.Enabled = true;
		AddTarget(commandCenter.PauseCommand, PauseCommand);

		commandCenter.ChangePlaybackPositionCommand.Enabled = true;
		AddTarget(commandCenter.ChangePlaybackPositionCommand, SeekCommand);

		commandCenter.SeekBackwardCommand.Enabled = true;
		AddTarget(commandCenter.SeekBackwardCommand, SeekBackwardCommand);

		commandCenter.SeekForwardCommand.Enabled = false;
		AddTarget(commandCenter.SeekForwardCommand, SeekForwardCommand);
	}

	/// <summary>
	/// The metadata for the currently playing media.
	/// </summary>
	public MPNowPlayingInfo NowPlayingInfo { get; } = new();


	/// <summary>
	/// Clears the metadata for the currently playing media.
	/// </summary>
	public static void ClearNowPlaying()
	{
		nowPlayingInfoPublisher = null;
		MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = nowPlayingInfoDefault;
	}

	/// <summary>
	/// Sets the data for the currently playing media from the media element.
	/// </summary>
	/// <param name="playerItem"></param>
	/// <param name="mediaElement"></param>
	public void SetMetadata(AVPlayerItem? playerItem, IMediaElement? mediaElement)
	{
		if (mediaElement is null)
		{
			Metadata.ClearNowPlaying();
			return;
		}

		NowPlayingInfo.Title = mediaElement.MetadataTitle;
		NowPlayingInfo.Artist = mediaElement.MetadataArtist;
		NowPlayingInfo.PlaybackDuration = playerItem?.Duration.Seconds ?? 0;
		NowPlayingInfo.IsLiveStream = false;
		NowPlayingInfo.PlaybackRate = GetPlaybackRate(mediaElement.Speed);
		NowPlayingInfo.ElapsedPlaybackTime = playerItem?.CurrentTime.Seconds ?? 0;
		NowPlayingInfo.Artwork = CreateArtwork(ResolveArtwork(mediaElement), logger);
		PublishNowPlayingInfo();
	}

	public void PublishNowPlayingInfo()
	{
		nowPlayingInfoPublisher = this;
		MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = NowPlayingInfo;
	}

	public void PublishPlaybackRate(double speed)
	{
		NowPlayingInfo.PlaybackRate = GetPlaybackRate(speed);
		PublishNowPlayingInfo();
	}

	// A rate of 0 is how Now Playing shows paused media: the system advances the elapsed time on the lock screen and in Control Center at this rate
	double GetPlaybackRate(double speed) => player.TimeControlStatus is AVPlayerTimeControlStatus.Playing ? speed : 0;

	/// <summary>
	/// Unregisters from the shared <see cref="MPRemoteCommandCenter"/>, which would otherwise keep this instance alive and keep
	/// sending it commands for a player that is gone.
	/// </summary>
	public void Dispose()
	{
		// The command center is shared by every player of the app: only this instance's targets are removed
		foreach (var (command, target) in remoteCommandTargets)
		{
			command.RemoveTarget(target);
			target.Dispose();
		}

		remoteCommandTargets.Clear();

		// The lock screen and Control Center must not keep showing the media of a player that is gone; another player's media stays
		if (ReferenceEquals(nowPlayingInfoPublisher, this))
		{
			ClearNowPlaying();
		}
	}

	/// <summary>
	/// Resolves the location of the artwork a media element shows: <see cref="IMediaElement.MetadataArtworkSource"/> when it is set, else
	/// <see cref="IMediaElement.MetadataArtworkUrl"/>.
	/// </summary>
	/// <returns>The URL, or <see langword="null"/> when there is no artwork to load.</returns>
	internal static NSUrl? ResolveArtworkUrl(IMediaElement mediaElement)
	{
		var (artworkSource, metadataArtworkUrl) = MetadataArtworkResolver.Resolve(mediaElement);
		if (GetArtworkUrl(artworkSource) is NSUrl artworkUrl)
		{
			return artworkUrl;
		}

		// 17.0.0 gave MetadataArtworkUrl to NSURL as written, and its poster showed any URL that NSData reads (a data: URL, for example)
		return string.IsNullOrWhiteSpace(metadataArtworkUrl) ? null : NSUrl.FromString(metadataArtworkUrl);
	}

	/// <summary>
	/// Resolves the location of the artwork a <see cref="MediaSource"/> points to.
	/// </summary>
	/// <returns>The URL, or <see langword="null"/> for a source that cannot be an artwork (such as a stream) or that points to nothing.</returns>
	internal static NSUrl? GetArtworkUrl(MediaSource? artworkSource)
	{
		switch (artworkSource)
		{
			case UriMediaSource { Uri: Uri uri } when MetadataArtworkPaths.IsHttpUri(uri) || (uri.IsAbsoluteUri && uri.IsFile):
				return NSUrl.FromString(uri.AbsoluteUri);

			case FileMediaSource fileMediaSource:
				return MetadataArtworkPaths.GetLocalFilePath(fileMediaSource.Path) is string localFilePath
					? NSUrl.CreateFileUrl(localFilePath)
					: null;

			case ResourceMediaSource { Path: string path } when !string.IsNullOrWhiteSpace(path) && Path.HasExtension(path):
				// Same split as MediaManager.CreateAsset, so a resource resolves to the file the player would open
				return NSBundle.MainBundle.GetUrlForResource(
					Path.GetFileNameWithoutExtension(path),
					Path.GetExtension(path)[1..],
					Path.GetDirectoryName(path) ?? string.Empty);

			default:
				return null;
		}
	}

	/// <summary>
	/// Resolves what the artwork a media element shows is loaded from: <see cref="IMediaElement.MetadataArtworkSource"/> when it is set, else
	/// <see cref="IMediaElement.MetadataArtworkUrl"/>.
	/// </summary>
	/// <returns>The artwork, or <see langword="null"/> when there is none to load.</returns>
	internal ResolvedArtwork? ResolveArtwork(IMediaElement mediaElement)
	{
		var artworkSource = MetadataArtworkResolver.Resolve(mediaElement).Source;
		if (artworkSource is not StreamMediaSource)
		{
			streamArtwork = null;
		}

		switch (artworkSource)
		{
			// NSData cannot send headers: the artwork is downloaded with them, as AVURLAsset downloads Source with them
			case UriMediaSource { Uri: Uri uri, HttpHeaders.Count: > 0 } uriSource when MetadataArtworkPaths.IsHttpUri(uri):
				return NSUrl.FromString(uri.AbsoluteUri) is NSUrl url ? new ResolvedArtwork(url, [.. uriSource.HttpHeaders], null) : null;

			case StreamMediaSource { Stream: Stream stream }:
				return GetStreamArtworkData(stream, mediaElement.Source) is NSData data ? new ResolvedArtwork(null, [], data) : null;

			default:
				return ResolveArtworkUrl(mediaElement) is NSUrl artworkUrl ? new ResolvedArtwork(artworkUrl, [], null) : null;
		}
	}

	/// <summary>
	/// Loads the image of an artwork.
	/// </summary>
	/// <returns>The image, or <see langword="null"/> when the artwork cannot be read or is not an image, which is logged.</returns>
	internal static UIImage? LoadImage(ResolvedArtwork artwork, ILogger? logger)
	{
		try
		{
			var data = artwork switch
			{
				{ Data: NSData artworkData } => artworkData,
				{ Url: NSUrl url, HttpHeaders.Length: > 0 } => DownloadArtwork(url, artwork.HttpHeaders),
				{ Url: NSUrl url } => NSData.FromUrl(url),
				_ => null
			};

			var image = data is null ? null : UIImage.LoadFromData(data);

			// An image without a size has no aspect ratio, which Now Playing and the poster need to scale it
			if (image is null || image.Size.Width <= 0 || image.Size.Height <= 0)
			{
				logger?.LogWarning("Unable to load the metadata artwork {ArtworkUrl}: it cannot be read or is not an image", artwork.Url?.AbsoluteString ?? "from a stream");
				return null;
			}

			return image;
		}
		catch (Exception ex)
		{
			logger?.LogWarning(ex, "Unable to load the metadata artwork {ArtworkUrl}", artwork.Url?.AbsoluteString ?? "from a stream");
			return null;
		}
	}

	/// <summary>
	/// Loads the image of an artwork, logging to the logger of this instance when it cannot be loaded.
	/// </summary>
	internal UIImage? LoadImage(ResolvedArtwork artwork) => LoadImage(artwork, logger);

	// Like NSData.FromUrl, which the artwork without headers uses, the download blocks its thread. NSURLSession calls its completion handler on its
	// own queue, so waiting here, on the main thread too, cannot deadlock it.
	static NSData? DownloadArtwork(NSUrl url, KeyValuePair<string, string>[] httpHeaders)
	{
		using var request = new NSMutableUrlRequest(url)
		{
			Headers = NSDictionary.FromObjectsAndKeys(
				httpHeaders.Select(header => header.Value).ToArray<object>(),
				httpHeaders.Select(header => header.Key).ToArray<object>())
		};

		var download = new TaskCompletionSource<NSData?>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var dataTask = NSUrlSession.SharedSession.CreateDataTask(request, (data, response, error) =>
			download.TrySetResult(error is null && response is NSHttpUrlResponse { StatusCode: >= 200 and < 300 } ? data : null));
		dataTask.Resume();

		// The request times out after 60 seconds (NSURLRequest.timeoutInterval), which completes the task with an error before this wait ends
		if (!download.Task.Wait(TimeSpan.FromSeconds(90)))
		{
			dataTask.Cancel();
			return null;
		}

		return download.Task.Result;
	}

	NSData? GetStreamArtworkData(Stream stream, MediaSource? source)
	{
		if (MetadataArtworkLoader.IsStreamOfTheSource(stream, source))
		{
			logger?.LogWarning("{ArtworkSource} is not used: its stream is that of {Source}, which the player reads", nameof(IMediaElement.MetadataArtworkSource), nameof(IMediaElement.Source));
			return null;
		}

		if (streamArtwork is { } cachedArtwork && ReferenceEquals(cachedArtwork.Stream, stream))
		{
			return cachedArtwork.Data;
		}

		NSData? data = null;
		try
		{
			// The stream belongs to the app, which may use it again: it is read, not disposed
			var bytes = MetadataArtworkLoader.ReadStream(stream);
			data = bytes.Length > 0 ? NSData.FromArray(bytes) : null;
		}
		catch (Exception ex)
		{
			logger?.LogWarning(ex, "Unable to read the metadata artwork from its stream");
		}

		streamArtwork = (stream, data);
		return data;
	}

	// The artwork is resolved on the calling thread and captured alone: Now Playing outlives the media element, so the handler must not hold it
	static MPMediaItemArtwork CreateArtwork(ResolvedArtwork? artwork, ILogger? logger) =>
		new(boundsSize: new(320, 240), requestHandler: _ => (artwork is null ? null : LoadImage(artwork, logger)) ?? defaultUIImage);

	// The image that Now Playing shows for the artwork at this URL
	static UIImage LoadArtworkImage(NSUrl? artworkUrl) =>
		(artworkUrl is null ? null : LoadImage(new ResolvedArtwork(artworkUrl, [], null), logger: null)) ?? defaultUIImage;

	void AddTarget(MPRemoteCommand command, Func<MPRemoteCommandEvent, MPRemoteCommandHandlerStatus> handler) =>
		remoteCommandTargets.Add((command, command.AddTarget(handler)));

	MPRemoteCommandHandlerStatus SeekCommand(MPRemoteCommandEvent? commandEvent)
	{
		if (commandEvent is not MPChangePlaybackPositionCommandEvent eventArgs)
		{
			return MPRemoteCommandHandlerStatus.CommandFailed;
		}

		// A time scale of 1000 keeps the milliseconds of the requested position (a time scale of 1 rounds it to whole seconds)
		var seekTime = CMTime.FromSeconds(eventArgs.PositionTime, 1000);
		player.Seek(seekTime);
		return MPRemoteCommandHandlerStatus.Success;
	}

	MPRemoteCommandHandlerStatus SeekBackwardCommand(MPRemoteCommandEvent? commandEvent)
	{
		if (commandEvent is null)
		{
			return MPRemoteCommandHandlerStatus.CommandFailed;
		}

		var seekTime = player.CurrentTime - CMTime.FromSeconds(10, 1);
		player.Seek(seekTime);
		return MPRemoteCommandHandlerStatus.Success;
	}

	MPRemoteCommandHandlerStatus SeekForwardCommand(MPRemoteCommandEvent? commandEvent)
	{
		if (commandEvent is null)
		{
			return MPRemoteCommandHandlerStatus.CommandFailed;
		}

		var seekTime = player.CurrentTime + CMTime.FromSeconds(10, 1);
		player.Seek(seekTime);
		return MPRemoteCommandHandlerStatus.Success;
	}

	MPRemoteCommandHandlerStatus PlayCommand(MPRemoteCommandEvent? commandEvent)
	{
		if (commandEvent is null)
		{
			return MPRemoteCommandHandlerStatus.CommandFailed;
		}

		play();
		return MPRemoteCommandHandlerStatus.Success;
	}

	MPRemoteCommandHandlerStatus PauseCommand(MPRemoteCommandEvent? commandEvent)
	{
		if (commandEvent is null)
		{
			return MPRemoteCommandHandlerStatus.CommandFailed;
		}

		player.Pause();
		return MPRemoteCommandHandlerStatus.Success;
	}

	MPRemoteCommandHandlerStatus ToggleCommand(MPRemoteCommandEvent? commandEvent)
	{
		if (commandEvent is null)
		{
			return MPRemoteCommandHandlerStatus.CommandFailed;
		}

		if (player.Rate is 0)
		{
			play();
		}
		else
		{
			player.Pause();
		}

		return MPRemoteCommandHandlerStatus.Success;
	}
}