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
	/// <summary>
	/// 
	/// </summary>
	/// <param name="handle"></param>
	public TizenPlayer(IntPtr handle) : base(handle, (code, message) => { throw GetException(code, message); })
	{
	}
}

public partial class MediaManager : IDisposable
{
	VideoView? videoView;
	bool isUriStreaming;
	float playbackRate;

	/// <summary>
	/// Creates the corresponding platform view of <see cref="MediaElement"/> on Tizen.
	/// </summary>
	/// <returns>The platform native counterpart of <see cref="MediaElement"/>.</returns>
	public (PlatformMediaView platformView, VideoView videoView) CreatePlatformView()
	{
		var nativeWindow = mauiContext.Services.GetRequiredService<Tizen.NUI.Window>();
		videoView = new VideoView()
		{
			WidthSpecification = LayoutParamPolicies.MatchParent,
			HeightSpecification = LayoutParamPolicies.MatchParent,
		};
		videoView.Finished += VideoViewFinished;
		nativeWindow.Add(videoView);
		nativeWindow!.Remove(videoView);
		playbackRate = 1;
		player = GetPlayer(videoView);

		return (player, videoView);
	}

	void VideoViewFinished(object? sender, VideoView.FinishedEventArgs e)
	{
		mediaElement.MediaEnded();
		UpdateCurrentState();
	}

	TizenPlayer GetPlayer(VideoView videoView)
	{
		var handle = videoView.NativeHandle;
		if (handle.IsInvalid)
		{
			throw new InvalidOperationException();
		}
		var tizenPlayer = new TizenPlayer(handle.DangerousGetHandle());

		return tizenPlayer;
	}

	void UpdateCurrentState()
	{
		if (videoView is null)
		{
			return;
		}

		player = GetPlayer(videoView);
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
		if (player is null || mediaElement.Source is null)
		{
			return;
		}

		videoView?.Play();
		UpdateCurrentState();
	}

	protected virtual partial void PlatformPause()
	{
		if (player is null || mediaElement.Source is null)
		{
			return;
		}

		videoView?.Pause();
		UpdateCurrentState();
	}

	protected virtual partial void PlatformSeek(TimeSpan position)
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		player = GetPlayer(videoView!);
		if (player.State == PlayerState.Ready || player.State == PlayerState.Playing || player.State == PlayerState.Paused)
		{
			player.SetPlayPositionAsync((int)mediaElement.Position.TotalMilliseconds, false);
		}
	}

	protected virtual partial void PlatformStop()
	{
		if (player is null || mediaElement is null
			 || mediaElement.Source is null)
		{
			return;
		}

		videoView?.Stop();
		mediaElement.Position = TimeSpan.Zero;
		UpdateCurrentState();
	}

	

	protected virtual partial void PlatformUpdateSource()
	{
		var hasSetSource = false;

		if (player is null || videoView is null)
		{
			return;
		}

		if (mediaElement.Source is null)
		{
			videoView.ResourceUrl = null;
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
				videoView!.ResourceUrl = uri.ToString();
				isUriStreaming = true;
				hasSetSource = true;
			}
		}
		else if (mediaElement.Source is FileMediaSource fileMediaSource)
		{
			var filePath = fileMediaSource.Path;
			if (!string.IsNullOrWhiteSpace(filePath))
			{
				videoView!.ResourceUrl = ResourcePath.GetPath(filePath);

				isUriStreaming = false;
				hasSetSource = true;
			}
		}
		else if (mediaElement.Source is ResourceMediaSource resourceMediaSource)
		{
			var path = resourceMediaSource.Path;
			if (!string.IsNullOrWhiteSpace(path))
			{
				videoView!.ResourceUrl = ResourcePath.GetPath(path);
				isUriStreaming = false;
				hasSetSource = true;
			}
		}

		if (hasSetSource)
		{
			mediaElement.MediaOpened();
		}
	}

	protected virtual partial void PlatformUpdateSpeed()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}
		
		if (!isUriStreaming && mediaElement.Speed <= 5.0f && mediaElement.Speed >= -5.0f && mediaElement.Speed != 0
				&& playbackRate != (float)mediaElement.Speed)
		{
			player = GetPlayer(videoView!);
			if (player.State == PlayerState.Ready || player.State == PlayerState.Playing || player.State == PlayerState.Paused)
			{
				player.SetPlaybackRate((float)mediaElement.Speed);
				playbackRate = (float)mediaElement.Speed;
			}
		}
	}

	protected virtual partial void PlatformUpdateShowsPlaybackControls()
	{
		if (mediaElement is null || videoView is null)
		{
			return;
		}

		//videoView.UseController = mediaElement.ShowsPlaybackControls;
	}

	protected virtual partial void PlatformUpdatePosition()
	{
		if (mediaElement is null || player is null)
		{
			return;
		}

		player = GetPlayer(videoView!);
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

		player = GetPlayer(videoView!);
		player.Volume = (float)mediaElement.Volume;
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

		videoView.Looping = mediaElement.IsLooping;
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
				player.Dispose();
			}
			if (videoView is not null)
			{
				videoView.Finished -= VideoViewFinished;
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
