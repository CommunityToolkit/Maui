using AVFoundation;
using AVKit;
using Foundation;
using GameController;
using UIKit;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;

public class MauiMediaElement : UIView
{
	AVPlayer player;
	AVPlayerItem? playerItem;
	readonly AVPlayerViewController playerViewController;
	MediaElement? mediaElement;
	protected NSObject? playedToEndObserver;
	protected IDisposable? statusObserver;
	protected IDisposable? rateObserver;
	protected IDisposable? volumeObserver;

	public MauiMediaElement(MediaElement mediaElement)
	{
		this.mediaElement = mediaElement;

		playerViewController = new();
		
		player = new AVPlayer();
		playerViewController.Player = player;
		playerViewController.View!.Frame = Bounds;
		AddSubview(playerViewController.View);
	}

	public void UpdateSource()
	{
		AVAsset? asset = null;

		if (mediaElement?.Source is UriMediaSource uriSource)
		{
			if (uriSource.Uri?.Scheme is "ms-appx")
			{
				if (uriSource.Uri.LocalPath.Length <= 1)
				{
					return;
				}

				// used for a file embedded in the application package
				asset = AVAsset.FromUrl(NSUrl.FromFilename(uriSource.Uri.LocalPath.Substring(1)));
			}
			//TODO
			//else if (uriSource.Uri?.Scheme == "ms-appdata")
			//{
			//	var filePath = ResolveMsAppDataUri(uriSource.Uri);

			//	if (string.IsNullOrEmpty(filePath))
			//		throw new ArgumentException("Invalid Uri", "Source");

			//	asset = AVAsset.FromUrl(NSUrl.FromFilename(filePath));
			//}
			else if (uriSource.Uri != null)
			{
				var nsUrl = NSUrl.FromString(uriSource.Uri.AbsoluteUri) ??
							throw new NullReferenceException("NSUrl is null");
				asset = AVUrlAsset.Create(nsUrl);
			}
			else
			{
				throw new InvalidOperationException($"{nameof(uriSource.Uri)} is not initialized");
			}
		}
		else
		{
			if (mediaElement?.Source is FileMediaSource fileSource)
			{
				asset = AVAsset.FromUrl(NSUrl.FromFilename(fileSource.File));
			}
		}

		_ = asset ?? throw new NullReferenceException();

		playerItem = new AVPlayerItem(asset);
		AddStatusObserver();

		if (playerViewController.Player != null)
		{
			playerViewController.Player.ReplaceCurrentItemWithPlayerItem(playerItem);
		}
		else
		{
			playerViewController.Player = new AVPlayer(playerItem);
			AddRateObserver();
			AddVolumeObserver();
		}

		UpdateVolume();

		// TODO
		if (mediaElement?.AutoPlay ?? false)
		{
			player.Play();
		}
		else
		{
			playerViewController.Player?.Pause();
			playerViewController.Player?.ReplaceCurrentItemWithPlayerItem(null);
			DestroyStatusObserver();
			// TODO
			//Controller.CurrentState = MediaElementState.Stopped;
		}
	}

	public void UpdateSpeed()
	{
		if (playerViewController.Player == null || mediaElement == null)
		{
			return;
		}

		playerViewController.Player.Rate = (float)mediaElement.Speed;
	}

	public void UpdateVolume()
	{
		if (playerViewController.Player == null || mediaElement == null)
		{
			return;
		}

		playerViewController.Player.Volume = (float)mediaElement.Volume;
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

	void AddVolumeObserver()
	{
		DestroyVolumeObserver();
		volumeObserver = playerViewController.Player?.AddObserver("volume", NSKeyValueObservingOptions.New,
			ObserveVolume);
	}

	void AddRateObserver()
	{
		DestroyRateObserver();
		rateObserver = playerViewController.Player?.AddObserver("rate", NSKeyValueObservingOptions.New,
			ObserveRate);
	}

	void AddStatusObserver()
	{
		DestroyStatusObserver();
		statusObserver = playerItem?.AddObserver("status", NSKeyValueObservingOptions.New, ObserveStatus);
	}

	void AddPlayedToEndObserver()
	{
		DestroyPlayedToEndObserver();
		playedToEndObserver =
			NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, PlayedToEnd);
	}

	void DestroyVolumeObserver() => DisposeObservers(ref volumeObserver);

	void DestroyRateObserver() => DisposeObservers(ref rateObserver);

	void DestroyStatusObserver() => DisposeObservers(ref statusObserver);

	void DestroyPlayedToEndObserver()
	{
		if (playedToEndObserver == null)
		{
			return;
		}
		NSNotificationCenter.DefaultCenter.RemoveObserver(playedToEndObserver);
		DisposeObservers(ref playedToEndObserver);
	}

	protected void ObserveStatus(NSObservedChange e)
	{
		// TODO
		_ = playerViewController.Player?.CurrentItem ?? throw new NullReferenceException();
		_ = mediaElement ?? throw new NullReferenceException();
		mediaElement.Volume = playerViewController.Player.Volume;

		switch (playerViewController.Player.Status)
		{
			case AVPlayerStatus.Failed:
				// TODO add media failed event
				//mediaElement.OnMediaFailed();
				break;

			case AVPlayerStatus.ReadyToPlay:
				var duration = playerViewController.Player.CurrentItem.Duration;
				if (duration.IsIndefinite)
				{
					mediaElement.Duration = TimeSpan.Zero;
				}
				else
				{
					mediaElement.Duration = TimeSpan.FromSeconds(duration.Seconds);
				}

				// TODO add properties and events
				mediaElement.VideoHeight = (int)playerViewController.Player.CurrentItem.Asset.NaturalSize.Height;
				mediaElement.VideoWidth = (int)playerViewController.Player.CurrentItem.Asset.NaturalSize.Width;
				//mediaElement.OnMediaOpened();
				mediaElement.Position = Position;
				break;
		}
	}

	protected virtual void ObserveRate(NSObservedChange e)
	{
		// TODO
		//if (mediaElement is object)
		//{
		//	switch (playerViewController.Player?.Rate)
		//	{
		//		case 0.0f:
		//			mediaElement.CurrentState = MediaElementState.Paused;
		//			break;

		//		default:
		//			mediaElement.CurrentState = MediaElementState.Playing;
		//			break;
		//	}

		//	Controller.Position = Position;
		//}
	}

	void ObserveVolume(NSObservedChange e)
	{
		if (mediaElement == null || playerViewController?.Player == null)
		{
			return;
		}

		mediaElement.Volume = playerViewController.Player.Volume;
	}

	void PlayedToEnd(NSNotification notification)
	{
		// TODO
		//if (mediaElement == null || notification.Object != playerViewController.Player?.CurrentItem)
		//{
		//	return;
		//}

		//if (mediaElement.IsLooping)
		//{
		//	playerViewController.Player?.Seek(CMTime.Zero);
		//	Controller.Position = Position;
		//	playerViewController.Player?.Play();
		//}
		//else
		//{
		//	SetKeepScreenOn(false);
		//	Controller.Position = Position;

		//	try
		//	{
		//		Device.BeginInvokeOnMainThread(Controller.OnMediaEnded);
		//	}
		//	catch (Exception e)
		//	{
		//		Log.Warning("MediaElement", $"Failed to play media to end: {e}");
		//	}
		//}
	}

	TimeSpan Position
	{
		get
		{
			if (playerViewController.Player?.CurrentItem == null)
			{
				return TimeSpan.Zero;
			}

			var currentTime = playerViewController.Player.CurrentTime;

			if (double.IsNaN(currentTime.Seconds) || currentTime.IsIndefinite)
			{
				return TimeSpan.Zero;
			}

			return TimeSpan.FromSeconds(currentTime.Seconds);
		}
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
}
