using Microsoft.Extensions.Logging;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System.Display;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.MediaElement;

partial class MediaManager : IDisposable
{
	// The requests to keep display active are cumulative, this bool makes sure it only gets requested once
	bool displayActiveRequested;
	readonly DisplayRequest displayRequest = new();

	protected bool isMediaPlayerAttached;

	public PlatformMediaView CreatePlatformView()
	{
		player = new();
		return player;
	}

	protected virtual partial void PlatformPlay()
	{
		if (!isMediaPlayerAttached || player is null)
		{
			return;
		}
		
		player.MediaPlayer.Play();

		if (mediaElement.KeepScreenOn) 
		{
			displayRequest.RequestActive();
			displayActiveRequested = true;
		}
	}

	protected virtual partial void PlatformPause()
	{
		if (!isMediaPlayerAttached || player is null)
		{
			return;
		}
		
		player.MediaPlayer.Pause();

		if (displayActiveRequested)
		{
			displayRequest.RequestRelease();
			displayActiveRequested = false;
		}
	}

	protected virtual partial void PlatformStop()
	{
		if (!isMediaPlayerAttached || player is null)
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
		if (!isMediaPlayerAttached || player is null)
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

		var hasSetSource = false;

		player.AutoPlay = mediaElement.AutoPlay;

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
		else if (mediaElement.Source is ResourceMediaSource)
		{
			string path = "ms-appx:///" + (mediaElement.Source as ResourceMediaSource)!.Path!;
			if (!string.IsNullOrWhiteSpace(path))
			{
				player.Source = WinMediaSource.CreateFromUri(new Uri(path));
				hasSetSource = true;
			}
		}

		if (hasSetSource && !isMediaPlayerAttached)
		{
			Dispatcher.GetForCurrentThread()?.DispatchAsync(() =>
			{
				player.MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
			});
		}

		if (hasSetSource && mediaElement.KeepScreenOn && !displayActiveRequested)
		{
			displayRequest.RequestActive();
			displayActiveRequested = true;
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

		Dispatcher.GetForCurrentThread()?.DispatchAsync(() =>
		{
			mediaElement.Duration = player.MediaPlayer.NaturalDuration;
			mediaElement.MediaOpened();

			if (!isMediaPlayerAttached)
			{
				isMediaPlayerAttached = true;

				player.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
				player.MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
				player.MediaPlayer.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted;
				player.MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
				player.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
			}
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

	void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
	{
		if (isMediaPlayerAttached && mediaElement is not null)
		{
			Dispatcher.GetForCurrentThread()?.DispatchAsync(() =>
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
				displayRequest.RequestRelease();
				displayActiveRequested = false;

				player.MediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
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