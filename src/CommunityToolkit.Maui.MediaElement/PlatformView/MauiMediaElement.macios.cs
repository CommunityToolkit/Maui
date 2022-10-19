using System.Diagnostics;
using AVFoundation;
using AVKit;
using CoreMedia;
using Foundation;
using Microsoft.Maui.Controls;
using UIKit;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;


public class MauiMediaElement : UIView
{
	protected NSObject? playedToEndObserver;

	readonly AVPlayer player;
	AVPlayerItem? playerItem;
	readonly AVPlayerViewController playerViewController;
	MediaElement? mediaElement;

	public MauiMediaElement(MediaElement mediaElement)
	{
		this.mediaElement = mediaElement;

		playerViewController = new AVPlayerViewController();

		player = new AVPlayer();
		playerViewController.Player = player;

		playerViewController.View!.Frame = this.Bounds;
		AddSubview(playerViewController.View);

		AddPlayedToEndObserver();
	}

	void AddPlayedToEndObserver()
	{
		DestroyPlayedToEndObserver();

		playedToEndObserver =
			NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, PlayedToEnd);
	}

	void PlayedToEnd(NSNotification notification)
	{
		if (mediaElement is null || notification.Object != playerViewController.Player?.CurrentItem)
		{
			return;
		}

		if (mediaElement.IsLooping)
		{
			playerViewController.Player?.Seek(CMTime.Zero);
			player.Play();
		}
		else
		{
			// TODO Implement KeepScreenOn
			//SetKeepScreenOn(false);

			try
			{
				mediaElement.Dispatcher.Dispatch(mediaElement.OnMediaEnded);
			}
			catch (Exception e)
			{
				// TODO inject ILogger everywhere and report there?
				//Log.Warning("MediaElement", $"Failed to play media to end: {e}");
			}
		}
	}

	void DestroyPlayedToEndObserver()
	{
		if (playedToEndObserver is not null)
		{
			NSNotificationCenter.DefaultCenter.RemoveObserver(playedToEndObserver);
			DisposeObservers(ref playedToEndObserver);
		}
	}

	protected void DisposeObservers(ref IDisposable? disposable)
	{
		disposable?.Dispose();
		disposable = null;
	}

	protected void DisposeObservers(ref NSObject? disposable)
	{
		disposable?.Dispose();
		disposable = null;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (player != null)
			{
				player.ReplaceCurrentItemWithPlayerItem(null);
				player.Dispose();
			}

			if (playerViewController != null)
			{
				playerViewController.Dispose();
			}

			mediaElement = null;
		}

		base.Dispose(disposing);
	}

	public void UpdateTransportControlsEnabled()
	{
		if (mediaElement is null)
		{
			return;
		}

		playerViewController.ShowsPlaybackControls = mediaElement.ShowsPlaybackControls;
	}

	public void UpdateSource()
	{
		AVAsset? asset = null;

		if (mediaElement!.Source is UriMediaSource)
		{
			string uri = (mediaElement.Source as UriMediaSource)!.Uri!.AbsoluteUri;

			if (!string.IsNullOrWhiteSpace(uri))
			{
				asset = AVAsset.FromUrl(new NSUrl(uri));
			}
		}
		else if (mediaElement.Source is FileMediaSource)
		{
			string uri = (mediaElement.Source as FileMediaSource)!.File!;

			if (!string.IsNullOrWhiteSpace(uri))
			{
				asset = AVAsset.FromUrl(NSUrl.CreateFileUrl(new[] { uri }));
			}
		}

		if (asset != null)
		{
			playerItem = new AVPlayerItem(asset);
		}
		else
		{
			playerItem = null;
		}

		player.ReplaceCurrentItemWithPlayerItem(playerItem);
		if (playerItem != null && mediaElement.AutoPlay)
		{
			player.Play();
		}
	}

	public void UpdatePosition()
	{
		if (mediaElement is null)
		{
			return;
		}

		TimeSpan controlPosition = ConvertTime(player.CurrentTime);
		if (Math.Abs((controlPosition - mediaElement.Position).TotalSeconds) > 1)
		{
			player.Seek(CMTime.FromSeconds(mediaElement.Position.TotalSeconds, 1));
		}
	}

	public void UpdateShowsPlaybackControls()
	{
		if (playerViewController.Player is null || mediaElement is null)
		{
			return;
		}

		playerViewController.ShowsPlaybackControls = mediaElement.ShowsPlaybackControls;
	}

	public void UpdateSpeed()
	{
		if (playerViewController.Player is null || mediaElement is null)
		{
			return;
		}

		playerViewController.Player.Rate = (float)mediaElement.Speed;
	}

	public void UpdateStatus()
	{
		if (mediaElement is null)
		{
			return;
		}

		var videoStatus = MediaElementState.Closed;

		switch (player.Status)
		{
			case AVPlayerStatus.ReadyToPlay:
				switch (player.TimeControlStatus)
				{
					case AVPlayerTimeControlStatus.Playing:
						videoStatus = MediaElementState.Playing;
						break;

					case AVPlayerTimeControlStatus.Paused:
						videoStatus = MediaElementState.Paused;
						break;
				}
				break;
		}

		mediaElement.CurrentState = videoStatus;

		if (playerItem != null)
		{
			mediaElement.Duration = ConvertTime(playerItem.Duration);
			mediaElement.Position = ConvertTime(playerItem.CurrentTime);
		}
		else
		{
			player.Pause();
			mediaElement.Duration = mediaElement.Position = TimeSpan.Zero;
		}
	}

	public void UpdateVolume()
	{
		if (playerViewController.Player is null || mediaElement is null)
		{
			return;
		}

		playerViewController.Player.Volume = (float)mediaElement.Volume;
	}

	public void PlayRequested(TimeSpan position)
	{
		// TODO do something with position?

		player.Play();
	}

	public void PauseRequested(TimeSpan position)
	{
		// TODO do something with position?
		player.Pause();
	}

	public void StopRequested(TimeSpan position)
	{
		player.Pause();
		player.Seek(new CMTime(0, 1));
	}

	TimeSpan ConvertTime(CMTime cmTime)
	{
		return TimeSpan.FromSeconds(Double.IsNaN(cmTime.Seconds) ? 0 : cmTime.Seconds);
	}
}