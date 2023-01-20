using System;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Compatibility.Platform.Tizen;
using Tizen.Multimedia;
using Tizen.NUI.BaseComponents;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MediaManager : IDisposable
{
	/// <summary>
	/// The platform native counterpart of <see cref="MediaElement"/>.
	/// </summary>
	protected VideoView? videoView;

	/// <summary>
	/// Indicates whether <see cref="videoView"/> is initialized.
	/// </summary>
	protected bool isPlayerInitialized;

	/// <summary>
	/// Indicates whether the loaded <see cref="MediaElement.Source"/> is streaming from a URI.
	/// </summary>
	protected bool isUriStreaming;

	/// <summary>
	/// Indicates whether the device's screen is locked.
	/// </summary>
	protected bool isScreenLocked;

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaElement"/> on Tizen.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaElement"/>.</returns>
	public VideoView CreatePlatformView()
	{
		videoView = new VideoView()
		{
			WidthSpecification = LayoutParamPolicies.MatchParent,
			HeightSpecification = LayoutParamPolicies.MatchParent
		};

		videoView.AddedToWindow += AddedToWindow;

		return videoView;
	}

	void AddedToWindow(object? sender, EventArgs e)
	{
		if (!isPlayerInitialized && videoView is not null)
		{
			InitializePlayer(videoView);
			isPlayerInitialized = true;
		}

		if (videoView is not null)
		{
			videoView.AddedToWindow -= AddedToWindow;
		}
	}

	void OnPlaybackCompleted(object? sender, EventArgs e)
	{
		mediaElement.MediaEnded();
		mediaElement.CurrentStateChanged(MediaElementState.Stopped);
	}

	void OnErrorOccurred(object? sender, PlayerErrorOccurredEventArgs e)
	{
		mediaElement.MediaFailed(new MediaFailedEventArgs(e.Error.ToString()));
	}

	void OnBufferingProgressChanged(object? sender, BufferingProgressChangedEventArgs e)
	{
		if (e.Percent is 100)
		{
			UpdateCurrentState();
		}
		else
		{
			mediaElement.CurrentStateChanged(MediaElementState.Buffering);
		}
	}

	void InitializePlayer(VideoView videoView)
	{
		var handle = videoView.NativeHandle;
		if (handle.IsInvalid)
		{
			throw new InvalidOperationException("The NativeHandler is invalid");
		}

		if (player is null)
		{
			player = new TizenPlayer(handle.DangerousGetHandle());
			player.InitializePlayer();
			player.PlaybackCompleted += OnPlaybackCompleted;
			player.ErrorOccurred += OnErrorOccurred;
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
			PlayerState.Idle => MediaElementState.None,
			PlayerState.Ready => MediaElementState.Opening,
			PlayerState.Playing => MediaElementState.Playing,
			PlayerState.Paused => MediaElementState.Paused,
			_ => MediaElementState.None
		};

		mediaElement.CurrentStateChanged(newsState);
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
			mediaElement.SeekCompleted();
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
			mediaElement.Position = TimeSpan.Zero;
			mediaElement.CurrentStateChanged(MediaElementState.Stopped);
		}

		mediaElement.Position = TimeSpan.Zero;
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

		if (mediaElement.Source is null)
		{
			player.SetSource(null);
			mediaElement.Duration = TimeSpan.Zero;
			mediaElement.CurrentStateChanged(MediaElementState.None);
			return;
		}

		mediaElement.CurrentStateChanged(MediaElementState.Opening);

		if (mediaElement.Source is UriMediaSource uriMediaSource)
		{
			var uri = uriMediaSource.Uri;
			if (!string.IsNullOrWhiteSpace(uri?.AbsoluteUri))
			{
				player.SetSource(new MediaUriSource(uri.AbsoluteUri));
				isUriStreaming = true;
			}
		}
		else if (mediaElement.Source is FileMediaSource fileMediaSource)
		{
			var path = fileMediaSource.Path;
			if (!string.IsNullOrWhiteSpace(path))
			{
				player.SetSource(new MediaUriSource(path));
				isUriStreaming = false;
			}
		}
		else if (mediaElement.Source is ResourceMediaSource resourceMediaSource)
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
			mediaElement.MediaOpened();
		}
	}

	protected virtual partial void PlatformUpdateSpeed()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		if (!isUriStreaming && mediaElement.Speed <= 5.0f && mediaElement.Speed >= -5.0f && mediaElement.Speed != 0)
		{
			if (player.State is PlayerState.Ready || player.State is PlayerState.Playing || player.State is PlayerState.Paused)
			{
				player.SetPlaybackRate((float)mediaElement.Speed);
			}
		}
	}

	protected virtual partial void PlatformUpdateShouldShowPlaybackControls()
	{
		if (mediaElement is null || videoView is null)
		{
			return;
		}
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		if (player.State is PlayerState.Ready || player.State is PlayerState.Playing || player.State is PlayerState.Paused)
		{
			mediaElement.Duration = TimeSpan.FromMilliseconds(player.StreamInfo.GetDuration());
			mediaElement.Position = TimeSpan.FromMilliseconds(player.GetPlayPosition());
		}
		else
		{
			mediaElement.Duration = mediaElement.Position = TimeSpan.Zero;
		}
	}
	
	protected virtual partial void PlatformUpdateVolume()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		if (mediaElement.Volume >= 0.0 && mediaElement.Volume <= 1.0)
		{
			player.Volume = (float)mediaElement.Volume;
		}
	}

	protected virtual partial void PlatformUpdateShouldKeepScreenOn()
	{
		if (videoView is null)
		{
			return;
		}

		if (mediaElement.ShouldKeepScreenOn && !isScreenLocked)
		{
			Tizen.System.Power.RequestLock(Tizen.System.PowerLock.DisplayNormal, 0);
			Tizen.System.Power.RequestLock(Tizen.System.PowerLock.Cpu, 0);
			isScreenLocked = true;
		}
		else if (!mediaElement.ShouldKeepScreenOn && isScreenLocked)
		{
			Tizen.System.Power.ReleaseLock(Tizen.System.PowerLock.DisplayNormal);
			Tizen.System.Power.ReleaseLock(Tizen.System.PowerLock.Cpu);
			isScreenLocked = false;
		}
	}

	protected virtual partial void PlatformUpdateShouldLoopPlayback()
	{
		if (mediaElement is null || player is null || videoView is null)
		{
			return;
		}

		player.IsLooping = mediaElement.ShouldLoopPlayback;

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
				player.ErrorOccurred -= OnErrorOccurred;
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
