using AVFoundation;
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

	// The instance whose media the shared MPNowPlayingInfoCenter shows, if any
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

		var url = mediaElement.MetadataArtworkUrl;

		NowPlayingInfo.Title = mediaElement.MetadataTitle;
		NowPlayingInfo.Artist = mediaElement.MetadataArtist;
		NowPlayingInfo.PlaybackDuration = playerItem?.Duration.Seconds ?? 0;
		NowPlayingInfo.IsLiveStream = false;
		NowPlayingInfo.PlaybackRate = mediaElement.Speed;
		NowPlayingInfo.ElapsedPlaybackTime = playerItem?.CurrentTime.Seconds ?? 0;
		NowPlayingInfo.Artwork = new(boundsSize: new(320, 240), requestHandler: _ => GetImage(url));
		PublishNowPlayingInfo();
	}

	/// <summary>
	/// Shows <see cref="NowPlayingInfo"/> on the shared <see cref="MPNowPlayingInfoCenter"/> (lock screen and Control Center).
	/// </summary>
	public void PublishNowPlayingInfo()
	{
		nowPlayingInfoPublisher = this;
		MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = NowPlayingInfo;
	}

	/// <summary>
	/// Removes the targets that this instance registered on the shared <see cref="MPRemoteCommandCenter"/>, so that the commands no
	/// longer reach a player that is gone and this instance can be collected, and clears the Now Playing info if it is this instance's.
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