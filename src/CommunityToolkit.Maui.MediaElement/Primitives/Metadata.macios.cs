using AVFoundation;
using CommunityToolkit.Maui.Core;
using CoreMedia;
using Foundation;
using MediaPlayer;
using UIKit;

namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// The class that provides methods to update the system UI for media transport controls to display media metadata.
/// </summary>
public class Metadata : IDisposable
{
	static readonly UIImage defaultUIImage = new();
	static readonly MPMediaItemArtwork defaultImage = new(new UIImage());
	static readonly MPNowPlayingInfo nowPlayingInfoDefault = new()
	{
		AlbumTitle = string.Empty,
		Title = string.Empty,
		Artist = string.Empty,
		PlaybackDuration = 0,
		IsLiveStream = false,
		PlaybackRate = 0,
		ElapsedPlaybackTime = 0,
		Artwork = defaultImage
	};

	readonly PlatformMediaElement player;

	bool isDisposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="Metadata"/> class.
	/// </summary>
	/// <param name="player"></param>
	public Metadata(PlatformMediaElement player)
	{
		this.player = player;
		MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = NowPlayingInfo;

		var commandCenter = MPRemoteCommandCenter.Shared;

		commandCenter.TogglePlayPauseCommand.Enabled = true;
		commandCenter.TogglePlayPauseCommand.AddTarget(ToggleCommand);

		commandCenter.PlayCommand.Enabled = true;
		commandCenter.PlayCommand.AddTarget(PlayCommand);

		commandCenter.PauseCommand.Enabled = true;
		commandCenter.PauseCommand.AddTarget(PauseCommand);

		commandCenter.ChangePlaybackPositionCommand.Enabled = true;
		commandCenter.ChangePlaybackPositionCommand.AddTarget(SeekCommand);

		commandCenter.SeekBackwardCommand.Enabled = true;
		commandCenter.SeekBackwardCommand.AddTarget(SeekBackwardCommand);

		commandCenter.SeekForwardCommand.Enabled = false;
		commandCenter.SeekForwardCommand.AddTarget(SeekForwardCommand);
	}

	/// <summary>
	/// The metadata for the currently playing media.
	/// </summary>
	public MPNowPlayingInfo NowPlayingInfo { get; } = new();

	/// <summary>
	/// Cleans up the resources used by the <see cref="Metadata"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Clears the metadata for the currently playing media.
	/// </summary>
	public void ClearNowPlaying() => MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = nowPlayingInfoDefault;

	/// <summary>
	/// Sets the data for the currently playing media from the media element.
	/// </summary>
	/// <param name="playerItem"></param>
	/// <param name="mediaElement"></param>
	public void SetMetadata(AVPlayerItem? playerItem, IMediaElement? mediaElement)
	{
		if (mediaElement is null)
		{
			ClearNowPlaying();
			return;
		}

		NowPlayingInfo.Title = mediaElement.MetadataTitle;
		NowPlayingInfo.Artist = mediaElement.MetadataArtist;
		NowPlayingInfo.PlaybackDuration = playerItem?.Duration.Seconds ?? 0;
		NowPlayingInfo.IsLiveStream = false;
		NowPlayingInfo.PlaybackRate = mediaElement.Speed;
		NowPlayingInfo.ElapsedPlaybackTime = playerItem?.CurrentTime.Seconds ?? 0;
		NowPlayingInfo.Artwork = GetArtwork(mediaElement.MetadataArtworkUrl);
		MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = NowPlayingInfo;
	}

	/// <summary>
	/// Cleans up the resources used by the <see cref="Metadata"/>.
	/// </summary>
	/// <param name="disposing"></param>
	protected virtual void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				defaultImage.Dispose();
			}

			isDisposed = true;
		}
	}

	static MPMediaItemArtwork GetArtwork(string? imageUri)
	{
		try
		{
			if (!string.IsNullOrWhiteSpace(imageUri))
			{
				return new MPMediaItemArtwork(GetImage(imageUri));
			}

			return defaultImage;
		}
		catch
		{
			return defaultImage;
		}
	}

	static UIImage GetImage(string imageUri)
	{
		if (imageUri.StartsWith(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
		{
			return UIImage.LoadFromData(NSData.FromUrl(new NSUrl(imageUri))) ?? defaultUIImage;
		}

		return defaultUIImage;
	}

	MPRemoteCommandHandlerStatus SeekCommand(MPRemoteCommandEvent? commandEvent)
	{
		if (commandEvent is not MPChangePlaybackPositionCommandEvent eventArgs)
		{
			return MPRemoteCommandHandlerStatus.CommandFailed;
		}

		var seekTime = CMTime.FromSeconds(eventArgs.PositionTime, 1);
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

		player.Play();
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
		if (commandEvent is not null)
		{
			return MPRemoteCommandHandlerStatus.CommandFailed;
		}

		if (player.Rate is 0)
		{
			player.Play();
		}
		else
		{
			player.Pause();
		}

		return MPRemoteCommandHandlerStatus.Success;
	}
}