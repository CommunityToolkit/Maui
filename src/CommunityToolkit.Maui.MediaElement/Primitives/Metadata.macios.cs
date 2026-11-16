using AVFoundation;
using CommunityToolkit.Maui.Views;
using CoreMedia;
using Foundation;
using MediaPlayer;
using UIKit;

namespace CommunityToolkit.Maui.Core;

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
	readonly List<(MPRemoteCommand Command, NSObject Target)> remoteCommandTargets = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="Metadata"/> class.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="play">Starts playback at the requested speed (<see cref="AVPlayer.Play"/> starts at <see cref="AVPlayer.DefaultRate"/>, which only exists from iOS 16 and Mac Catalyst 16).</param>
	public Metadata(PlatformMediaElement player, Action play)
	{
		this.player = player;
		this.play = play;
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

#pragma warning disable CS0618 // MetadataArtworkUrl stays supported: it is used when MetadataArtworkSource is not set
		var url = mediaElement.MetadataArtworkUrl;
#pragma warning restore CS0618

		NowPlayingInfo.Title = mediaElement.MetadataTitle;
		NowPlayingInfo.Artist = mediaElement.MetadataArtist;
		NowPlayingInfo.PlaybackDuration = playerItem?.Duration.Seconds ?? 0;
		NowPlayingInfo.IsLiveStream = false;
		NowPlayingInfo.PlaybackRate = GetPlaybackRate(mediaElement.Speed);
		NowPlayingInfo.ElapsedPlaybackTime = playerItem?.CurrentTime.Seconds ?? 0;
		NowPlayingInfo.Artwork = mediaElement.MetadataArtworkSource is MediaSource artworkSource
			? CreateArtwork(GetArtworkUrl(artworkSource))
			: new(boundsSize: new(320, 240), requestHandler: _ => GetImage(url));
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

	// The location is resolved on the calling thread and captured alone: Now Playing outlives the media element, so the handler must not hold it
	static MPMediaItemArtwork CreateArtwork(NSUrl? artworkUrl) =>
		new(boundsSize: new(320, 240), requestHandler: _ => LoadArtworkImage(artworkUrl));

	static UIImage LoadArtworkImage(NSUrl? artworkUrl)
	{
		if (artworkUrl is null)
		{
			return defaultUIImage;
		}

		try
		{
			var image = NSData.FromUrl(artworkUrl) is NSData data ? UIImage.LoadFromData(data) : null;

			// An image without a size has no aspect ratio, which Now Playing needs to scale it
			return image is null || image.Size.Width <= 0 || image.Size.Height <= 0 ? defaultUIImage : image;
		}
		catch
		{
			return defaultUIImage;
		}
	}

	static UIImage GetImage(string imageUri)
	{
		try
		{
			if (imageUri.StartsWith(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
			{
				return UIImage.LoadFromData(NSData.FromUrl(new NSUrl(imageUri))) ?? defaultUIImage;
			}
			return defaultUIImage;
		}
		catch
		{
			return defaultUIImage;
		}
	}

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