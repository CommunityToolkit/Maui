using AVFoundation;
using CommunityToolkit.Maui.Views;
using CoreMedia;
using Foundation;
using MediaPlayer;
using UIKit;

namespace CommunityToolkit.Maui.Core;

sealed class Metadata
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

	readonly PlatformMediaElement player;

	/// <summary>
	/// Initializes a new instance of the <see cref="Metadata"/> class.
	/// </summary>
	/// <param name="player"></param>
	public Metadata(PlatformMediaElement player)
	{
		this.player = player;
		MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = nowPlayingInfoDefault;

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
	/// Clears the metadata for the currently playing media.
	/// </summary>
	public static void ClearNowPlaying() => MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = nowPlayingInfoDefault;

	/// <summary>
	/// Sets the data for the currently playing media from the media element.
	/// </summary>
	/// <param name="playerItem"></param>
	/// <param name="mediaElement"></param>
	public async Task SetMetadata(AVPlayerItem? playerItem, IMediaElement? mediaElement)
	{
		if (mediaElement is null)
		{
			return;
		}
		ClearNowPlaying();
		var artwork = await MetadataArtworkMediaSource(mediaElement.MetadataArtworkSource).ConfigureAwait(false);

		if (artwork is UIImage image)
		{
			NowPlayingInfo.Artwork = new(boundsSize: new(320, 240), requestHandler: _ => image);
		}
		else
		{
			NowPlayingInfo.Artwork = new(boundsSize: new(0, 0), requestHandler: _ => defaultUIImage);
		}
		NowPlayingInfo.Title = mediaElement.MetadataTitle;
		NowPlayingInfo.Artist = mediaElement.MetadataArtist;
		NowPlayingInfo.PlaybackDuration = playerItem?.Duration.Seconds ?? 0;
		NowPlayingInfo.IsLiveStream = false;
		NowPlayingInfo.PlaybackRate = mediaElement.Speed;
		NowPlayingInfo.ElapsedPlaybackTime = playerItem?.CurrentTime.Seconds ?? 0;
		MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = NowPlayingInfo;
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
		if (commandEvent is null)
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

	public static async Task<UIImage?> MetadataArtworkMediaSource(MediaSource? artworkUrl, CancellationToken cancellationToken = default)
	{
		switch(artworkUrl)
		{
			case UriMediaSource uriMediaSource:
				var uri = uriMediaSource.Uri;
				return GetBitmapFromUrl(uri?.AbsoluteUri);
			case FileMediaSource fileMediaSource:
				var uriFile = fileMediaSource.Path;
				return await GetBitmapFromFile(uriFile, cancellationToken).ConfigureAwait(false);
			case ResourceMediaSource resourceMediaSource:
				var path = resourceMediaSource.Path;
				return await GetBitmapFromResource(path, cancellationToken).ConfigureAwait(false);
			case null:
				return null;
		}
		return null;
	}

	static async Task<UIImage?> GetBitmapFromFile(string? resource, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(resource))
		{
			return null;
		}
		using var fileStream = File.OpenRead(resource);
		using var memoryStream = new MemoryStream();
		await fileStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
		memoryStream.Position = 0;
		NSData temp = NSData.FromStream(memoryStream) ?? new NSData();
		return UIImage.LoadFromData(temp);
	}
	static UIImage? GetBitmapFromUrl(string? resource)
	{
		if (string.IsNullOrEmpty(resource))
		{
			return null;
		}
		return UIImage.LoadFromData(NSData.FromUrl(new NSUrl(resource)));
	}
	static async Task<UIImage?> GetBitmapFromResource(string? resource, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(resource))
		{
			return null;
		}
		using var inputStream = await FileSystem.OpenAppPackageFileAsync(resource).ConfigureAwait(false);
		using var memoryStream = new MemoryStream();
		if (inputStream is null)
		{
			System.Diagnostics.Trace.WriteLine($"{inputStream} is null.");
			return null;
		}
		await inputStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
		memoryStream.Position = 0;
		NSData? nsdata = NSData.FromStream(memoryStream);
		if (nsdata is null)
		{
			System.Diagnostics.Trace.TraceInformation($"{nsdata} is null.");
			return null;
		}
		return UIImage.LoadFromData(nsdata);
	}
}