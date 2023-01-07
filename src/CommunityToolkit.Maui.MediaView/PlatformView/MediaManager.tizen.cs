using Microsoft.Maui.Controls.Compatibility.Platform.Tizen;
using Tizen.Multimedia;
using Tizen.NUI.BaseComponents;

namespace CommunityToolkit.Maui.MediaView;

public partial class MediaManager : IDisposable
{
	VideoView? videoView;
	bool isPlayerInitialized;
	bool isUriStreaming;
	bool isScreenLocked;

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaView"/> on Tizen.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaView"/>.</returns>
	public VideoView CreatePlatformView()
	{
		videoView = new VideoView()
		{
			WidthSpecification = LayoutParamPolicies.MatchParent,
			HeightSpecification = LayoutParamPolicies.MatchParent
		};
		videoView.AddedToWindow += (s, e) =>
		{
			if (!isPlayerInitialized)
			{
				InitializePlayer(videoView);
				isPlayerInitialized = true;
			}
		};

		return videoView;
	}

	void OnPlaybackCompleted(object? sender, EventArgs e)
	{
		mediaView.MediaEnded();
		mediaView.CurrentStateChanged(MediaViewState.Stopped);
	}

	void OnErrorOccured(object? sender, PlayerErrorOccurredEventArgs e)
	{
		mediaView.MediaFailed(new MediaFailedEventArgs(e.Error.ToString()));
	}

	void OnBufferingProgressChanged(object? sender, BufferingProgressChangedEventArgs e)
	{
		if (e.Percent is 100)
		{
			UpdateCurrentState();
		}
		else
		{
			mediaView.CurrentStateChanged(MediaViewState.Buffering);
		}
	}

	void InitializePlayer(VideoView videoView)
	{
		var handle = videoView.NativeHandle;
		if (handle.IsInvalid)
		{
			throw new InvalidOperationException();
		}

		if (player == null)
		{
			player = new TizenPlayer(handle.DangerousGetHandle());
			player.InitializePlayer();
			player.PlaybackCompleted += OnPlaybackCompleted;
			player.ErrorOccurred += OnErrorOccured;
			player.BufferingProgressChanged += OnBufferingProgressChanged;
			PlatformUpdateSource();
		}
	}

	void UpdateCurrentState()
	{
		if (player is null)
		{
			throw new InvalidOperationException("TizenPlayer must not be null.");
		}

		var newsState = player.State switch
		{
			PlayerState.Idle => MediaViewState.None,
			PlayerState.Ready => MediaViewState.Opening,
			PlayerState.Playing => MediaViewState.Playing,
			PlayerState.Paused => MediaViewState.Paused,
			_ => MediaViewState.None
		};

		mediaView.CurrentStateChanged(newsState);
	}

	protected virtual partial void PlatformPlay()
	{
		if (player is null)
		{
			return;
		}

		if (player.State is PlayerState.Ready || player.State is PlayerState.Paused)
		{
			player.Start();
			UpdateCurrentState();
		}
	}

	protected virtual partial void PlatformPause()
	{
		if (player is null)
		{
			return;
		}

		if (player.State is PlayerState.Playing)
		{
			player.Pause();
			UpdateCurrentState();
		}
	}

	protected virtual partial void PlatformSeek(TimeSpan position)
	{
		if (player is null)
		{
			return;
		}

		if (player.State is PlayerState.Ready || player.State is PlayerState.Playing || player.State is PlayerState.Paused)
		{
			player.SetPlayPositionAsync((int)position.TotalMilliseconds, false);
			mediaView.SeekCompleted();
		}
	}

	protected virtual partial void PlatformStop()
	{
		if (player is null)
		{
			return;
		}

		if (player.State is PlayerState.Playing || player.State is PlayerState.Paused)
		{
			player.Stop();
			mediaView.Position = TimeSpan.Zero;
			mediaView.CurrentStateChanged(MediaViewState.Stopped);
		}

		mediaView.Position = TimeSpan.Zero;
	}

	async void PreparePlayer()
	{
		if (player is not null)
		{
			await player.PrepareAsync();
			PlatformUpdatePosition();
			UpdateCurrentState();
		}
	}

	protected virtual partial void PlatformUpdateSource()
	{
		if (player is null)
		{
			return;
		}

		if (player.State is not PlayerState.Idle)
		{
			player.Unprepare();
		}

		if (mediaView.Source is null)
		{
			player.SetSource(null);
			mediaView.Duration = TimeSpan.Zero;
			mediaView.CurrentStateChanged(MediaViewState.None);
			return;
		}

		mediaView.CurrentStateChanged(MediaViewState.Opening);

		if (mediaView.Source is UriMediaSource uriMediaSource)
		{
			var uri = uriMediaSource.Uri;
			if (!string.IsNullOrWhiteSpace(uri?.AbsoluteUri))
			{
				player.SetSource(new MediaUriSource(uri.AbsoluteUri));
				isUriStreaming = true;
			}
		}
		else if (mediaView.Source is FileMediaSource fileMediaSource)
		{
			var path = fileMediaSource.Path;
			if (!string.IsNullOrWhiteSpace(path))
			{
				player.SetSource(new MediaUriSource(path));
				isUriStreaming = false;
			}
		}
		else if (mediaView.Source is ResourceMediaSource resourceMediaSource)
		{
			var path = resourceMediaSource.Path;
			if (!string.IsNullOrWhiteSpace(path))
			{
				player.SetSource(new MediaUriSource(ResourcePath.GetPath(path)));
				isUriStreaming = false;
			}
		}

		if (player.IsSourceSet)
		{
			PreparePlayer();
			mediaView.MediaOpened();
		}
	}

	protected virtual partial void PlatformUpdateSpeed()
	{
		if (mediaView is null || player is null)
		{
			return;
		}

		if (!isUriStreaming && mediaView.Speed <= 5.0f && mediaView.Speed >= -5.0f && mediaView.Speed != 0)
		{
			if (player.State is PlayerState.Ready || player.State is PlayerState.Playing || player.State is PlayerState.Paused)
			{
				player.SetPlaybackRate((float)mediaView.Speed);
			}
		}
	}

	protected virtual partial void PlatformUpdateShouldShowPlaybackControls()
	{
		if (mediaView is null || videoView is null)
		{
			return;
		}
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (mediaView is null || player is null)
		{
			return;
		}

		if (player.State is PlayerState.Ready || player.State is PlayerState.Playing || player.State is PlayerState.Paused)
		{
			mediaView.Duration = TimeSpan.FromMilliseconds(player.StreamInfo.GetDuration());
			mediaView.Position = TimeSpan.FromMilliseconds(player.GetPlayPosition());
		}
		else
		{
			mediaView.Duration = mediaView.Position = TimeSpan.Zero;
		}
	}
	
	protected virtual partial void PlatformUpdateVolume()
	{
		if (mediaView is null || player is null)
		{
			return;
		}

		if (mediaView.Volume >= 0.0 && mediaView.Volume <= 1.0)
		{
			player.Volume = (float)mediaView.Volume;
		}
	}

	protected virtual partial void PlatformUpdateShouldKeepScreenOn()
	{
		if (videoView is null)
		{
			return;
		}

		if (mediaView.ShouldKeepScreenOn && !isScreenLocked)
		{
			Tizen.System.Power.RequestLock(Tizen.System.PowerLock.DisplayNormal, 0);
			Tizen.System.Power.RequestLock(Tizen.System.PowerLock.Cpu, 0);
			isScreenLocked = true;
		}
		else if (!mediaView.ShouldKeepScreenOn && isScreenLocked)
		{
			Tizen.System.Power.ReleaseLock(Tizen.System.PowerLock.DisplayNormal);
			Tizen.System.Power.ReleaseLock(Tizen.System.PowerLock.Cpu);
			isScreenLocked = false;
		}
	}

	protected virtual partial void PlatformUpdateShouldLoopPlayback()
	{
		if (mediaView is null || player is null || videoView is null)
		{
			return;
		}

		player.IsLooping = mediaView.ShouldLoopPlayback;

	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="MediaManager"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (player is not null)
			{
				player.PlaybackCompleted -= OnPlaybackCompleted;
				player.ErrorOccurred -= OnErrorOccured;
				player.BufferingProgressChanged -= OnBufferingProgressChanged;
				player.Dispose();
			}
			if (videoView is not null)
			{
				videoView.Dispose();
			}
		}
	}

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MediaManager"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
	}
}
