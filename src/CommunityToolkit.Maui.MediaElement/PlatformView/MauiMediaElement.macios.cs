using AVFoundation;
using AVKit;
using CoreMedia;
using Foundation;
using GameController;
using Microsoft.Maui.Controls;
using UIKit;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;

public class MauiMediaElement : UIView
{
	AVPlayer player;
	AVPlayerItem? playerItem;
	AVPlayerViewController playerViewController;
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
		playerViewController.View!.Frame = this.Bounds;
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

	void UpdateVolume()
	{
		// TODO
		//if (avPlayerViewController.Player != null)
		//{
		//	avPlayerViewController.Player.Volume = (float)Element.Volume;
		//}
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
		// TODO
		//DestroyVolumeObserver();
		//volumeObserver = playerViewController.Player?.AddObserver("volume", NSKeyValueObservingOptions.New,
		//		ObserveVolume);
	}

	void AddRateObserver()
	{
		// TODO
		//DestroyRateObserver();
		//rateObserver = playerViewController.Player?.AddObserver("rate", NSKeyValueObservingOptions.New,
		//		ObserveRate);
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
		//_ = playerViewController.Player?.CurrentItem ?? throw new NullReferenceException();
		//Controller.Volume = playerViewController.Player.Volume;

		//switch (playerViewController.Player.Status)
		//{
		//	case AVPlayerStatus.Failed:
		//		Controller.OnMediaFailed();
		//		break;

		//	case AVPlayerStatus.ReadyToPlay:
		//		var duration = playerViewController.Player.CurrentItem.Duration;
		//		if (duration.IsIndefinite)
		//			Controller.Duration = TimeSpan.Zero;
		//		else
		//			Controller.Duration = TimeSpan.FromSeconds(duration.Seconds);

		//		Controller.VideoHeight = (int)playerViewController.Player.CurrentItem.Asset.NaturalSize.Height;
		//		Controller.VideoWidth = (int)playerViewController.Player.CurrentItem.Asset.NaturalSize.Width;
		//		Controller.OnMediaOpened();
		//		Controller.Position = Position;
		//		break;
		//}
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
