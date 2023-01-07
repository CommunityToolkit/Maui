using Microsoft.Extensions.Logging;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System.Display;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.MediaView;

partial class MediaManager : IDisposable
{
	readonly DisplayRequest displayRequest = new();

	// The requests to keep display active are cumulative, this bool makes sure it only gets requested once
	bool displayActiveRequested;

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaView"/> on Windows.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaView"/>.</returns>
	public PlatformMediaView CreatePlatformView()
	{
		player = new();
		MediaPlayer mediaPlayer = new();
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

		if (MediaView.ShouldKeepScreenOn
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

		MediaView.CurrentStateChanged(MediaViewState.Stopped);

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
		
		player.MediaPlayer.PlaybackRate = MediaView.Speed;
	}

	protected virtual partial void PlatformUpdateShouldShowPlaybackControls()
	{
		if (player is null)
		{
			return;
		}

		player.AreTransportControlsEnabled =
			MediaView.ShouldShowPlaybackControls;
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (MediaView is not null && player is not null
			&& MediaView.CurrentState == MediaViewState.Playing)
		{
			MediaView.Position = player.MediaPlayer.Position;
		}
	}

	protected virtual partial void PlatformUpdateVolume()
	{
		if (player is not null)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				player.MediaPlayer.Volume = MediaView.Volume;
			});
		}
	}

	protected virtual partial void PlatformUpdateShouldKeepScreenOn()
	{
		if (MediaView is null)
		{
			return;
		}

		if (MediaView.ShouldKeepScreenOn)
		{
			if (MediaView?.CurrentState == MediaViewState.Playing
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
		if (MediaView is null || player is null)
		{
			return;
		}

		if (MediaView.Source is null)
		{
			player.Source = null;
			MediaView.CurrentStateChanged(MediaViewState.None);

			return;
		}

		player.AutoPlay = MediaView.ShouldAutoPlay;

		if (MediaView.Source is UriMediaSource uriMediaSource)
		{
			var uri = uriMediaSource.Uri?.AbsoluteUri;
			if (!string.IsNullOrWhiteSpace(uri))
			{
				player.Source = WinMediaSource.CreateFromUri(new Uri(uri));
			}
		}
		else if (MediaView.Source is FileMediaSource fileMediaSource)
		{
			var filename = fileMediaSource.Path;
			if (!string.IsNullOrWhiteSpace(filename))
			{
				StorageFile storageFile = await StorageFile.GetFileFromPathAsync(filename);
				player.Source = WinMediaSource.CreateFromStorageFile(storageFile);
			}
		}
		else if (MediaView.Source is ResourceMediaSource resourceMediaSource)
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
		if (MediaView is null || player is null)
		{
			return;
		}

		player.MediaPlayer.IsLoopingEnabled = MediaView.IsLooping;
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

	void OnMediaPlayerMediaOpened(MediaPlayer sender, object args)
	{
		if (MediaView is null || player is null)
		{
			return;
		}

		MainThread.BeginInvokeOnMainThread(() =>
		{
			MediaView.Duration = player.MediaPlayer.NaturalDuration == TimeSpan.MaxValue ?
				TimeSpan.Zero
				: player.MediaPlayer.NaturalDuration;

			MediaView.MediaOpened();
		});
	}

	void OnMediaPlayerMediaEnded(MediaPlayer sender, object args)
	{
		MediaView?.MediaEnded();
	}

	void OnMediaPlayerMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
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

		MediaView?.MediaFailed(new MediaFailedEventArgs(message));

		Logger?.LogError("{logMessage}", message);
	}

	void OnMediaPlayerVolumeChanged(MediaPlayer sender, object args)
	{
		if (MediaView is not null)
		{
			MediaView.Volume = sender.Volume;
		}
	}

	void OnPlaybackSessionPlaybackStateChanged(MediaPlaybackSession sender, object args)
	{
		var newState = sender.PlaybackState switch
		{
			MediaPlaybackState.Buffering => MediaViewState.Buffering,
			MediaPlaybackState.Playing => MediaViewState.Playing,
			MediaPlaybackState.Paused => MediaViewState.Paused,
			MediaPlaybackState.Opening => MediaViewState.Opening,
			_ => MediaViewState.None,
		};
		
		MediaView?.CurrentStateChanged(newState);
	}

	void OnPlaybackSessionSeekCompleted(MediaPlaybackSession sender, object args)
	{
		MediaView?.SeekCompleted();
	}
}