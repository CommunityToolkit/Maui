using Microsoft.Extensions.Logging;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System.Display;
using WinMediaSource = Windows.Media.Core.MediaSource;
using WindowsMediaPlayer = Windows.Media.Playback.MediaPlayer;

namespace CommunityToolkit.Maui.MediaPlayer;

partial class MediaManager : IDisposable
{
	readonly DisplayRequest displayRequest = new();

	// The requests to keep display active are cumulative, this bool makes sure it only gets requested once
	bool displayActiveRequested;

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaPlayer"/> on Windows.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaPlayer"/>.</returns>
	public PlatformMediaPlayer CreatePlatformView()
	{
		player = new();
		WindowsMediaPlayer mediaPlayer = new();
		mediaPlayer.MediaOpened += OnMediaPlayerMediaOpened;

		player.SetMediaPlayer(mediaPlayer);

		player.MediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackSessionPlaybackStateChanged;
		player.MediaPlayer.PlaybackSession.SeekCompleted += OnPlaybackSessionSeekCompleted;
		player.MediaPlayer.MediaFailed += OnMediaPlayerMediaFailed;
		player.MediaPlayer.MediaEnded += OnMediaPlayerMediaEnded;
		player.MediaPlayer.VolumeChanged += OnMediaPlayerVolumeChanged;

		return player;
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
		player?.MediaPlayer.Play();

		if (mediaPlayer.ShouldKeepScreenOn
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

		mediaPlayer.CurrentStateChanged(MediaPlayerState.Stopped);

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
		
		player.MediaPlayer.PlaybackRate = mediaPlayer.Speed;
	}

	protected virtual partial void PlatformUpdateShouldShowPlaybackControls()
	{
		if (player is null)
		{
			return;
		}

		player.AreTransportControlsEnabled =
			mediaPlayer.ShouldShowPlaybackControls;
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (mediaPlayer is not null && player is not null
			&& mediaPlayer.CurrentState == MediaPlayerState.Playing)
		{
			mediaPlayer.Position = player.MediaPlayer.Position;
		}
	}

	protected virtual partial void PlatformUpdateVolume()
	{
		if (player is not null)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				player.MediaPlayer.Volume = mediaPlayer.Volume;
			});
		}
	}

	protected virtual partial void PlatformUpdateShouldKeepScreenOn()
	{
		if (mediaPlayer is null)
		{
			return;
		}

		if (mediaPlayer.ShouldKeepScreenOn)
		{
			if (mediaPlayer?.CurrentState == MediaPlayerState.Playing
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
		if (mediaPlayer is null || player is null)
		{
			return;
		}

		if (mediaPlayer.Source is null)
		{
			player.Source = null;
			mediaPlayer.CurrentStateChanged(MediaPlayerState.None);

			return;
		}

		player.AutoPlay = mediaPlayer.ShouldAutoPlay;

		if (mediaPlayer.Source is UriMediaSource uriMediaSource)
		{
			var uri = uriMediaSource.Uri?.AbsoluteUri;
			if (!string.IsNullOrWhiteSpace(uri))
			{
				player.Source = WinMediaSource.CreateFromUri(new Uri(uri));
			}
		}
		else if (mediaPlayer.Source is FileMediaSource fileMediaSource)
		{
			var filename = fileMediaSource.Path;
			if (!string.IsNullOrWhiteSpace(filename))
			{
				StorageFile storageFile = await StorageFile.GetFileFromPathAsync(filename);
				player.Source = WinMediaSource.CreateFromStorageFile(storageFile);
			}
		}
		else if (mediaPlayer.Source is ResourceMediaSource resourceMediaSource)
		{
			string path = "ms-appx:///" + resourceMediaSource.Path;
			if (!string.IsNullOrWhiteSpace(path))
			{
				player.Source = WinMediaSource.CreateFromUri(new Uri(path));
			}
		}
	}

	protected virtual partial void PlatformUpdateShouldLoopPlayback()
	{
		if (mediaPlayer is null || player is null)
		{
			return;
		}

		player.MediaPlayer.IsLoopingEnabled = mediaPlayer.ShouldLoopPlayback;
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

				player.MediaPlayer.MediaOpened -= OnMediaPlayerMediaOpened;
				player.MediaPlayer.MediaFailed -= OnMediaPlayerMediaFailed;
				player.MediaPlayer.MediaEnded -= OnMediaPlayerMediaEnded;
				player.MediaPlayer.VolumeChanged -= OnMediaPlayerVolumeChanged;

				if (player.MediaPlayer.PlaybackSession is not null)
				{
					player.MediaPlayer.PlaybackSession.PlaybackStateChanged -= OnPlaybackSessionPlaybackStateChanged;
					player.MediaPlayer.PlaybackSession.SeekCompleted -= OnPlaybackSessionSeekCompleted;
				}
			}
		}
	}

	void OnMediaPlayerMediaOpened(WindowsMediaPlayer sender, object args)
	{
		if (mediaPlayer is null || player is null)
		{
			return;
		}

		MainThread.BeginInvokeOnMainThread(() =>
		{
			mediaPlayer.Duration = player.MediaPlayer.NaturalDuration == TimeSpan.MaxValue ?
				TimeSpan.Zero
				: player.MediaPlayer.NaturalDuration;

			mediaPlayer.MediaOpened();
		});
	}

	void OnMediaPlayerMediaEnded(WindowsMediaPlayer sender, object args)
	{
		mediaPlayer?.MediaEnded();
	}

	void OnMediaPlayerMediaFailed(WindowsMediaPlayer sender, MediaPlayerFailedEventArgs args)
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

		mediaPlayer?.MediaFailed(new MediaFailedEventArgs(message));

		Logger?.LogError("{logMessage}", message);
	}

	void OnMediaPlayerVolumeChanged(WindowsMediaPlayer sender, object args)
	{
		if (mediaPlayer is not null)
		{
			mediaPlayer.Volume = sender.Volume;
		}
	}

	void OnPlaybackSessionPlaybackStateChanged(MediaPlaybackSession sender, object args)
	{
		var newState = sender.PlaybackState switch
		{
			MediaPlaybackState.Buffering => MediaPlayerState.Buffering,
			MediaPlaybackState.Playing => MediaPlayerState.Playing,
			MediaPlaybackState.Paused => MediaPlayerState.Paused,
			MediaPlaybackState.Opening => MediaPlayerState.Opening,
			_ => MediaPlayerState.None,
		};

		mediaPlayer?.CurrentStateChanged(newState);
	}

	void OnPlaybackSessionSeekCompleted(MediaPlaybackSession sender, object args)
	{
		mediaPlayer?.SeekCompleted();
	}
}