using Microsoft.Maui.Controls.Compatibility.Platform.Tizen;
using Tizen.Multimedia;
using Tizen.NUI.BaseComponents;
using MPlayer = Tizen.Multimedia.Player;

namespace CommunityToolkit.Maui.MediaElement;

/// <summary>
///	
/// </summary>
public class TizenPlayer : MPlayer
{

	bool isInitialized;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="handle"></param>
	public TizenPlayer(IntPtr handle) : base(handle, (code, message) => { throw GetException(code, message); })
	{
	}

	/// <summary>
	/// 
	/// </summary>
	public void InitializePlayer()
	{
		if (!isInitialized)
		{
			Initialize();
			isInitialized = true;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public bool IsSourceSet => base.HasSource;
}

public partial class MediaManager : IDisposable
{
	VideoView? videoView;
	MediaSource? source;
	bool isPlayerPrepared;
	bool isUriStreaming;

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaElement"/> on Tizen.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaElement"/>.</returns>
	public (PlatformMediaView? platformView, VideoView videoView) CreatePlatformView()
	{
		var nativeWindow = mauiContext.Services.GetRequiredService<Tizen.NUI.Window>();
		videoView = new VideoView()
		{
			WidthSpecification = LayoutParamPolicies.MatchParent,
			HeightSpecification = LayoutParamPolicies.MatchParent
		};
		videoView.AddedToWindow += (s, e) =>
		{
			InitializePlayer(videoView);
		};

		return (null, videoView);
	}

	TizenPlayer GetPlayer(VideoView videoView)
	{
		var handle = videoView.NativeHandle;
		if (handle.IsInvalid)
		{
			throw new InvalidOperationException();
		}
		var tizenPlayer = new TizenPlayer(handle.DangerousGetHandle());
		tizenPlayer.InitializePlayer();

		tizenPlayer.PlaybackCompleted += OnPlaybackCompleted;
		tizenPlayer.ErrorOccurred += OnErrorOccured;
		tizenPlayer.BufferingProgressChanged += OnBufferingProgressChanged;

		return tizenPlayer;
	}

	void OnPlaybackCompleted(object? sender, EventArgs e)
	{
		mediaElement.MediaEnded();
		mediaElement.CurrentStateChanged(MediaElementState.Stopped);
	}

	void OnErrorOccured(object? sender, PlayerErrorOccurredEventArgs e)
	{
		mediaElement.MediaFailed(new MediaFailedEventArgs(e.Error.ToString()));
	}

	void OnBufferingProgressChanged(object? sender, BufferingProgressChangedEventArgs e)
	{
		if (e.Percent == 100)
		{
			mediaElement.CurrentStateChanged(MediaElementState.Opening);
		}
		else
		{
			mediaElement.CurrentStateChanged(MediaElementState.Buffering);
		}
	}

	void InitializePlayer(VideoView videoView)
	{
		player = GetPlayer(videoView);
		if (!player.IsSourceSet)
		{
			PlatformUpdateSource();
		}
	}

	void UpdateCurrentState()
	{
		if (player is null)
		{
			return;
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

		if (player.State == PlayerState.Ready || player.State == PlayerState.Paused)
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

		if (player.State == PlayerState.Playing)
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

		if (player.State == PlayerState.Ready || player.State == PlayerState.Playing || player.State == PlayerState.Paused)
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

		if (player.State == PlayerState.Playing || player.State == PlayerState.Paused)
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
			if (!string.IsNullOrWhiteSpace(uri?.AbsolutePath))
			{
				player.SetSource(new MediaUriSource(uri.ToString()));
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
			if (player.State == PlayerState.Ready || player.State == PlayerState.Playing || player.State == PlayerState.Paused)
			{
				player.SetPlaybackRate((float)mediaElement.Speed);
			}
		}
	}

	protected virtual partial void PlatformUpdateShowsPlaybackControls()
	{
		if (mediaElement is null || videoView is null)
		{
			return;
		}

		// Not Supported
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		if (player.State == PlayerState.Ready || player.State == PlayerState.Playing || player.State == PlayerState.Paused)
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

	protected virtual partial void PlatformUpdateKeepScreenOn()
	{
		if (videoView is null)
		{
			return;
		}

		//videoView.KeepScreenOn = mediaElement.KeepScreenOn;
	}

	protected virtual partial void PlatformUpdateIsLooping()
	{
		if (mediaElement is null || player is null || videoView is null)
		{
			return;
		}

		player.IsLooping = mediaElement.IsLooping;

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
