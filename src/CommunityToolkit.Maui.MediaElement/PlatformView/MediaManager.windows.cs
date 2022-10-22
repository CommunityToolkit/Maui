using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.MediaElement;

partial class MediaManager
{
	bool isMediaPlayerAttached;

	public PlatformMediaView CreatePlatformView()
	{
		player = new();
		return player;
	}

	protected virtual partial void PlatformPlay(TimeSpan timeSpan)
	{
		if (isMediaPlayerAttached && player is not null)
		{
			player.MediaPlayer.Play();
		}
	}

	protected virtual partial void PlatformPause(TimeSpan timeSpan)
	{
		if (isMediaPlayerAttached && player is not null)
		{
			player.MediaPlayer.Pause();
		}
	}

	protected virtual partial void PlatformStop(TimeSpan timeSpan)
	{
		if (isMediaPlayerAttached && player is not null)
		{
			// There's no Stop method so pause the video and reset its position
			player.MediaPlayer.Pause();
			player.MediaPlayer.Position = TimeSpan.Zero;
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
		if (mediaElement is null || player is null)
		{
			return;
		}

		if (isMediaPlayerAttached)
		{
			MediaElementState status = MediaElementState.Closed;

			switch (player.MediaPlayer.CurrentState)
			{
				case MediaPlayerState.Playing:
					status = MediaElementState.Playing;
					break;
				case MediaPlayerState.Paused:
				case MediaPlayerState.Stopped:
					status = MediaElementState.Paused;
					break;
			}

			mediaElement.CurrentState = status;
			mediaElement.Position = player.MediaPlayer.Position;
		}
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
			player.MediaPlayer.MediaOpened += OnMediaPlayerMediaOpened;
		}

		if (hasSetSource && mediaElement.AutoPlay)
		{
			player.AutoPlay = true;
		}
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
		});
	}
}
