
using AVFoundation;
using CommunityToolkit.Maui.Core;
using Foundation;
using MediaPlayer;
using UIKit;
using CoreMedia;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// The class that provides methods to update the system UI for media transport controls to display media metadata.
/// </summary>
public class MetaDataExtensions
{
	/// <summary>
	/// The metadata for the currently playing media.
	/// </summary>
	public readonly MPNowPlayingInfo NowPlayingInfo;
	readonly PlatformMediaElement? player;

	/// <summary>
	/// Initializes a new instance of the <see cref="MetaDataExtensions"/> class.
	/// </summary>
	/// <param name="player"></param>
	public MetaDataExtensions(PlatformMediaElement player)
	{
		this.player = player;
		NowPlayingInfo = new();
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

	MPRemoteCommandHandlerStatus SeekCommand(MPRemoteCommandEvent commandEvent)
	{
		var eventArgs = commandEvent as MPChangePlaybackPositionCommandEvent;
		if (eventArgs is not null)
		{
			var seekTime = CMTime.FromSeconds(eventArgs.PositionTime, 1);
			player?.Seek(seekTime);
		}
		return MPRemoteCommandHandlerStatus.Success;
	}
	
	MPRemoteCommandHandlerStatus SeekBackwardCommand(MPRemoteCommandEvent commandEvent)
	{
		if (player is not null)
		{
			var seekTime = player.CurrentTime - CMTime.FromSeconds(10, 1);
			player.Seek(seekTime);
		}
		return MPRemoteCommandHandlerStatus.Success;
	}

	MPRemoteCommandHandlerStatus SeekForwardCommand(MPRemoteCommandEvent commandEvent)
	{
		if (player is not null)
		{
			var seekTime = player.CurrentTime + CMTime.FromSeconds(10, 1);
			player.Seek(seekTime);
		}
		return MPRemoteCommandHandlerStatus.Success;
	}

	MPRemoteCommandHandlerStatus PlayCommand(MPRemoteCommandEvent commandEvent)
	{
		player?.Play();
		return MPRemoteCommandHandlerStatus.Success;
	}

	MPRemoteCommandHandlerStatus PauseCommand(MPRemoteCommandEvent commandEvent)
	{
		player?.Pause();
		return MPRemoteCommandHandlerStatus.Success;
	}

	MPRemoteCommandHandlerStatus ToggleCommand(MPRemoteCommandEvent commandEvent)
	{
		if (player?.Rate == 0)
		{
			player?.Play();
		}
		else
		{
			player?.Pause();
		}
		return MPRemoteCommandHandlerStatus.Success;
	}

	/// <summary>
	/// Clears the metadata for the currently playing media.
	/// </summary>
	public void ClearNowPlaying()
	{
		NowPlayingInfo.AlbumTitle = string.Empty;
		NowPlayingInfo.Title = string.Empty;
		NowPlayingInfo.Artist = string.Empty;
		NowPlayingInfo.AlbumTitle = string.Empty;
		NowPlayingInfo.PlaybackDuration = 0;
		NowPlayingInfo.IsLiveStream = false;
		NowPlayingInfo.PlaybackRate = 0;
		NowPlayingInfo.ElapsedPlaybackTime = 0;
		NowPlayingInfo.Artwork = new MPMediaItemArtwork(new UIImage());
		MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = NowPlayingInfo;
	}


	/// <summary>
	/// Sets the data for the currently playing media from the media element.
	/// </summary>
	/// <param name="PlayerItem"></param>
	/// <param name="MediaElement"></param>
	public void SetMetaData(AVPlayerItem? PlayerItem, IMediaElement MediaElement)
	{
		if (MediaElement is null)
		{
			ClearNowPlaying();
			return;
		}
		NowPlayingInfo.Title = MediaElement.MetaDataTitle;
		NowPlayingInfo.Artist = MediaElement.MetaDataArtist;
		NowPlayingInfo.PlaybackDuration = PlayerItem?.Duration.Seconds ?? 0;
		NowPlayingInfo.IsLiveStream = false;
		NowPlayingInfo.PlaybackRate = MediaElement.Speed;
		NowPlayingInfo.ElapsedPlaybackTime = PlayerItem?.CurrentTime.Seconds ?? 0;
		NowPlayingInfo.Artwork = GetArtwork(MediaElement.MetaDataArtworkUrl);
		MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = NowPlayingInfo;
	}

	static MPMediaItemArtwork GetArtwork(string? ImageUri)
	{
		var defaultImage = new MPMediaItemArtwork(new UIImage());
		try
		{
			if (!string.IsNullOrWhiteSpace(ImageUri))
			{
				UIImage? image = GetImage(ImageUri);
				if (image is not null)
				{
					return new MPMediaItemArtwork(image);
				}
				return defaultImage;
			}
			return defaultImage;
		}
		catch
		{
			return defaultImage;
		}
		
	}

	static UIImage GetImage(string ImageUri)
	{
		return ImageUri switch
		{
			_ when ImageUri.StartsWith("http", StringComparison.CurrentCulture) 
			=> (UIImage.LoadFromData(NSData.FromUrl(new NSUrl(ImageUri))) ?? new UIImage()),
			_ => new UIImage()
		};
	}
}

