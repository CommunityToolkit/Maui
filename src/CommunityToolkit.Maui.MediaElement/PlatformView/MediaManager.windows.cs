using Microsoft.Extensions.Logging;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System.Display;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.MediaElement;

partial class MediaManager : IDisposable
{
	readonly DisplayRequest displayRequest = new();

	// The requests to keep display active are cumulative, this bool makes sure it only gets requested once
	bool displayActiveRequested;

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaElement"/> on Windows.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaElement"/>.</returns>
	public PlatformMediaView CreatePlatformView()
	{
		player = new();
		MediaPlayer mediaPlayer = new();
		mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;

		player.SetMediaPlayer(mediaPlayer);

		player.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
		player.MediaPlayer.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted;
		player.MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
		player.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
		player.MediaPlayer.VolumeChanged += MediaPlayer_VolumeChanged;

		return player;
	}

	protected virtual partial void PlatformPlay()
	{
		player?.MediaPlayer.Play();

		if (mediaElement.KeepScreenOn
			&& !displayActiveRequested) 
		{
			displayRequest.RequestActive();
			displayActiveRequested = true;
		}
	}

	protected virtual partial void PlatformPause()
	{
		player?.MediaPlayer.Pause();

		if (displayActiveRequested)
		{
			displayRequest.RequestRelease();
			displayActiveRequested = false;
		}
	}

	protected virtual partial void PlatformSeek(TimeSpan position)
	{
		if (player?.MediaPlayer.CanSeek ?? false)
		{
			player.MediaPlayer.Position = position;
		}
	}

	protected virtual partial void PlatformStop()
	{
		if (player is null)
		{
			return;
		}

		// There's no Stop method so pause the video and reset its position
		player.MediaPlayer.Pause();
		player.MediaPlayer.Position = TimeSpan.Zero;

		mediaElement.CurrentStateChanged(MediaElementState.Stopped);

		if (displayActiveRequested)
		{
			displayRequest.RequestRelease();
			displayActiveRequested = false;
		}
	}

	protected virtual partial void PlatformUpdateSpeed()
	{
		if (player is null)
		{
			return;
		}
		
		player.MediaPlayer.PlaybackRate = mediaElement.Speed;
	}

	protected virtual partial void PlatformUpdateShowsPlaybackControls()
	{
		if (player is null)
		{
			return;
		}

		player.AreTransportControlsEnabled =
			mediaElement.ShowsPlaybackControls;
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (mediaElement is not null && player is not null
			&& mediaElement.CurrentState == MediaElementState.Playing)
		{
			mediaElement.Position = player.MediaPlayer.Position;
		}
	}

	protected virtual partial void PlatformUpdateVolume()
	{
		if (player is not null)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				player.MediaPlayer.Volume = mediaElement.Volume;
			});
		}
	}

	protected virtual partial void PlatformUpdateKeepScreenOn()
	{
		if (mediaElement is null)
		{
			return;
		}

		if (mediaElement.KeepScreenOn)
		{
			if (mediaElement?.CurrentState == MediaElementState.Playing
				&& !displayActiveRequested)
			{
				displayRequest.RequestActive();
				displayActiveRequested = true;
			}
		} 
		else
		{
			if (displayActiveRequested)
			{
				displayRequest.RequestRelease();
				displayActiveRequested = false;
			}
		}
	}

	protected virtual async partial void PlatformUpdateSource()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		if (mediaElement.Source is null)
		{
			player.Source = null;
			mediaElement.CurrentStateChanged(MediaElementState.None);

			return;
		}

		player.AutoPlay = mediaElement.AutoPlay;

		if (mediaElement.Source is UriMediaSource)
		{
			var uri = (mediaElement.Source as UriMediaSource)?.Uri?.AbsoluteUri;
			if (!string.IsNullOrWhiteSpace(uri))
			{
				player.Source = WinMediaSource.CreateFromUri(new Uri(uri));
			}
		}
		else if (mediaElement.Source is FileMediaSource)
		{
			string filename = (mediaElement.Source as FileMediaSource)?.Path;
			if (!string.IsNullOrWhiteSpace(filename))
			{
				StorageFile storageFile = await StorageFile.GetFileFromPathAsync(filename);
				player.Source = WinMediaSource.CreateFromStorageFile(storageFile);
			}
		}
		else if (mediaElement.Source is ResourceMediaSource)
		{
			string path = "ms-appx:///" + (mediaElement.Source as ResourceMediaSource).Path;
			if (!string.IsNullOrWhiteSpace(path))
			{
				player.Source = WinMediaSource.CreateFromUri(new Uri(path));
			}
		}
	}

	protected virtual partial void PlatformUpdateIsLooping()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		player.MediaPlayer.IsLoopingEnabled = mediaElement.IsLooping;
	}

	void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		MainThread.BeginInvokeOnMainThread(() =>
		{
			mediaElement.Duration = player.MediaPlayer.NaturalDuration == TimeSpan.MaxValue ?
				TimeSpan.Zero
				: player.MediaPlayer.NaturalDuration;

			mediaElement.MediaOpened();
		});
	}

	void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
	{
		mediaElement?.MediaEnded();
	}

	void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
	{
		string errorMessage = string.Empty;
		string errorCode = string.Empty;
		string error = args.Error.ToString();

		if (!string.IsNullOrWhiteSpace(args.ErrorMessage))
		{
			errorMessage = $"Error message: {args.ErrorMessage}";
		}

		if (args.ExtendedErrorCode != null)
		{
			errorCode = $"Error code: {args.ExtendedErrorCode.Message}";
		}

		var message = string.Join(", ",
			new[] { error, errorCode, errorMessage }
			.Where(s => !string.IsNullOrEmpty(s)));

		mediaElement?.MediaFailed(new MediaFailedEventArgs(message));

		Logger?.LogError("{logMessage}", message);
	}

	void MediaPlayer_VolumeChanged(MediaPlayer sender, object args)
	{
		if (mediaElement is not null)
		{
			mediaElement.Volume = sender.Volume;
		}
	}

	void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
	{
		var newState = sender.PlaybackState switch
		{
			MediaPlaybackState.Buffering => MediaElementState.Buffering,
			MediaPlaybackState.Playing => MediaElementState.Playing,
			MediaPlaybackState.Paused => MediaElementState.Paused,
			MediaPlaybackState.Opening => MediaElementState.Opening,
			_ => MediaElementState.None,
		};
		
		mediaElement?.CurrentStateChanged(newState);
	}

	void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
	{
		mediaElement?.SeekCompleted();
	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="MediaManager"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (player?.MediaPlayer is not null)
			{
				if (displayActiveRequested)
				{
					displayRequest.RequestRelease();
					displayActiveRequested = false;
				}

				player.MediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
				player.MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
				player.MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
				player.MediaPlayer.VolumeChanged -= MediaPlayer_VolumeChanged;

				if (player.MediaPlayer.PlaybackSession is not null)
				{
					player.MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
					player.MediaPlayer.PlaybackSession.SeekCompleted -= PlaybackSession_SeekCompleted;
				}
			}
		}
	}

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MediaManager"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}