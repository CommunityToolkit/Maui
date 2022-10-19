using Microsoft.UI.Xaml.Controls;
using Windows.Media.Playback;
using Windows.Storage;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;

public class MauiMediaElement : Grid, IDisposable
{
	bool isDisposed;
	MediaPlayerElement? mediaPlayerElement;
	readonly MediaElement mediaElement;
	bool isMediaPlayerAttached;

	public MauiMediaElement(MediaElement mediaElement)
	{
		this.mediaElement = mediaElement;
		mediaPlayerElement = new MediaPlayerElement();
		Children.Add(mediaPlayerElement);
	}

	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (isDisposing)
		{
			if (isMediaPlayerAttached)
			{
				if (mediaPlayerElement is not null)
				{
					mediaPlayerElement.MediaPlayer.MediaOpened -= OnMediaPlayerMediaOpened;
					mediaPlayerElement.MediaPlayer.Dispose();
				}
			}

			mediaPlayerElement = null;
		}

		isDisposed = true;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public void UpdatePosition()
	{
		if (mediaPlayerElement is null)
		{
			return;
		}

		if (isMediaPlayerAttached)
		{
			if (Math.Abs((mediaPlayerElement.MediaPlayer.Position - mediaElement.Position).TotalSeconds) > 1)
			{
				mediaPlayerElement.MediaPlayer.Position = mediaElement.Position;
			}
		}
	}

	public void UpdateShowsPlaybackControls()
	{
		if (mediaPlayerElement is null)
		{
			return;
		}

		mediaPlayerElement.AreTransportControlsEnabled = mediaElement.ShowsPlaybackControls;
	}

	public async void UpdateSource()
	{
		if (mediaElement is null || mediaPlayerElement is null)
		{
			return;
		}

		bool hasSetSource = false;

		if (mediaElement.Source is UriMediaSource)
		{
			string uri = (mediaElement.Source as UriMediaSource)?.Uri?.AbsoluteUri!;
			if (!string.IsNullOrWhiteSpace(uri))
			{
				mediaPlayerElement.Source = WinMediaSource.CreateFromUri(new Uri(uri));
				hasSetSource = true;
			}
		}
		else if (mediaElement.Source is FileMediaSource)
		{
			string filename = (mediaElement.Source as FileMediaSource)?.File!;
			if (!string.IsNullOrWhiteSpace(filename))
			{
				StorageFile storageFile = await StorageFile.GetFileFromPathAsync(filename);
				mediaPlayerElement.Source = WinMediaSource.CreateFromStorageFile(storageFile);
				hasSetSource = true;
			}
		}
		// TODO
		//else if (_video.Source is ResourceVideoSource)
		//{
		//	string path = "ms-appx:///" + (_video.Source as ResourceVideoSource).Path;
		//	if (!string.IsNullOrWhiteSpace(path))
		//	{
		//		_mediaPlayerElement.Source = MediaSource.CreateFromUri(new Uri(path));
		//		hasSetSource = true;
		//	}
		//}

		if (hasSetSource && !isMediaPlayerAttached)
		{
			isMediaPlayerAttached = true;
			mediaPlayerElement.MediaPlayer.MediaOpened += OnMediaPlayerMediaOpened;
		}

		if (hasSetSource && mediaElement.AutoPlay)
		{
			mediaPlayerElement.AutoPlay = true;
		}
	}

	public void UpdateSpeed()
	{
	}

	public void UpdateStatus()
	{
		if (mediaElement is null || mediaPlayerElement is null)
		{
			return;
		}

		if (isMediaPlayerAttached)
		{
			MediaElementState status = MediaElementState.Closed;

			switch (mediaPlayerElement.MediaPlayer.CurrentState)
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
			mediaElement.Position = mediaPlayerElement.MediaPlayer.Position;
		}
	}

	public void UpdateVolume()
	{
	}

	void OnMediaPlayerMediaOpened(MediaPlayer sender, object args)
	{
		if (mediaElement is null || mediaPlayerElement is null)
		{
			return;
		}

		MainThread.BeginInvokeOnMainThread(() =>
		{
			mediaElement.Duration = mediaPlayerElement.MediaPlayer.NaturalDuration;
		});
	}
}