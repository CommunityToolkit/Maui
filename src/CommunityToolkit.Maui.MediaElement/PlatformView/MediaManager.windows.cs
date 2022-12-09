using Windows.Media.Playback;
using Windows.Storage;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.MediaElement;

partial class MediaManager : IDisposable
{
	protected bool isMediaPlayerAttached;

	public PlatformMediaView CreatePlatformView()
	{
		player = new();
		return player;
	}

	protected virtual partial void PlatformPlay()
	{
		if (isMediaPlayerAttached && player is not null)
		{
			player.MediaPlayer.Play();
		}
	}

	protected virtual partial void PlatformPause()
	{
		if (isMediaPlayerAttached && player is not null)
		{
			player.MediaPlayer.Pause();
		}
	}

	protected virtual partial void PlatformStop()
	{
		if (isMediaPlayerAttached && player is not null)
		{
			// There's no Stop method so pause the video and reset its position
			player.MediaPlayer.Pause();
			player.MediaPlayer.Position = TimeSpan.Zero;

			mediaElement.CurrentStateChanged(MediaElementState.Stopped);
		}
	}

	protected virtual partial void PlatformUpdateSpeed()
	{
		if (isMediaPlayerAttached && player is not null)
		{
			player.MediaPlayer.PlaybackRate = mediaElement.Speed;
		}
	}

	protected virtual partial void PlatformUpdateShowsPlaybackControls()
	{
		if (player is null)
		{
			return;
		}

		player.AreTransportControlsEnabled = mediaElement.ShowsPlaybackControls;
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (isMediaPlayerAttached && player is not null)
		{
			if (Math.Abs((player.MediaPlayer.Position - mediaElement.Position).TotalSeconds) > 1)
			{
				player.MediaPlayer.Position = mediaElement.Position;
			}
		}
	}

	protected virtual partial void PlatformUpdateStatus()
	{
		// no-op
	}

	protected virtual partial void PlatformUpdateVolume()
	{
		if (isMediaPlayerAttached && player is not null)
		{
			player.MediaPlayer.Volume = mediaElement.Volume;
		}
	}

	protected virtual async partial void PlatformUpdateSource()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		var hasSetSource = false;

		if (mediaElement.Source is UriMediaSource)
		{
			var uri = (mediaElement.Source as UriMediaSource)?.Uri?.AbsoluteUri!;
			if (!string.IsNullOrWhiteSpace(uri))
			{
				player.Source = WinMediaSource.CreateFromUri(new Uri(uri));
				hasSetSource = true;
			}
		}
		else if (mediaElement.Source is FileMediaSource)
		{
			string filename = (mediaElement.Source as FileMediaSource)?.File!;
			if (!string.IsNullOrWhiteSpace(filename))
			{
				StorageFile storageFile = await StorageFile.GetFileFromPathAsync(filename);
				player.Source = WinMediaSource.CreateFromStorageFile(storageFile);
				hasSetSource = true;
			}
		}

		if (hasSetSource && !isMediaPlayerAttached)
		{
			isMediaPlayerAttached = true;
			MainThread.BeginInvokeOnMainThread(() =>
			{
				player.MediaPlayer.MediaOpened += OnMediaPlayerMediaOpened;
			});
		}

		if (hasSetSource && mediaElement.AutoPlay)
		{
			player.AutoPlay = true;
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

	void OnMediaPlayerMediaOpened(MediaPlayer sender, object args)
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		MainThread.BeginInvokeOnMainThread(() =>
		{
			mediaElement.Duration = player.MediaPlayer.NaturalDuration;
			mediaElement.MediaOpened();

			player.MediaPlayer.MediaOpened -= OnMediaPlayerMediaOpened;
			player.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
			player.MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
			player.MediaPlayer.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted;
			player.MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
			player.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
		});
	}

	void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
	{
		mediaElement?.MediaEnded();
	}

	void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

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

		mediaElement.MediaFailed(new MediaFailedEventArgs(
			string.Join(", ", new[] { error, errorCode, errorMessage }.Where(s => !string.IsNullOrEmpty(s)))));
	}

	void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
	{
		if (mediaElement is null)
		{
			return;
		}

		var newState = sender.PlaybackState switch
		{
			MediaPlaybackState.Buffering => MediaElementState.Buffering,
			MediaPlaybackState.Playing => MediaElementState.Playing,
			MediaPlaybackState.Paused => MediaElementState.Paused,
			MediaPlaybackState.Opening => MediaElementState.Opening,
			_ => MediaElementState.None,
		};
		
		mediaElement.CurrentStateChanged(newState);
	}

	void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
	{
		if (isMediaPlayerAttached)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				mediaElement.Position = sender.Position;
			});
		}
	}

	void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
	{
		mediaElement?.SeekCompleted();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (player?.MediaPlayer is not null)
			{
				player.MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
				player.MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;

				if (player.MediaPlayer.PlaybackSession is not null)
				{
					player.MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
					player.MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
					player.MediaPlayer.PlaybackSession.SeekCompleted -= PlaybackSession_SeekCompleted;
				}
			}
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}