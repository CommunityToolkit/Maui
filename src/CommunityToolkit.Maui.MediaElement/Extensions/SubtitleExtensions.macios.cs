using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Extensions;

partial class SubtitleExtensions : UIViewController
{
	readonly PlatformMediaElement player;
	readonly UIViewController playerViewController;
	readonly UILabel subtitleLabel;

	static readonly UIColor subtitleBackgroundColor = UIColor.FromRGBA(0, 0, 0, 128);
	static readonly UIColor clearBackgroundColor = UIColor.FromRGBA(0, 0, 0, 0);

	NSObject? playerObserver;
	UIViewController? viewController;

	public SubtitleExtensions(PlatformMediaElement player, UIViewController playerViewController)
	{
		this.playerViewController = playerViewController;
		this.player = player;
		Cues = [];
		subtitleLabel = new UILabel
		{
			Frame = CalculateSubtitleFrame(playerViewController),
			TextColor = UIColor.White,
			TextAlignment = UITextAlignment.Center,
			Font = UIFont.SystemFontOfSize(12),
			Text = "",
			Lines = 0,
			LineBreakMode = UILineBreakMode.WordWrap,
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth
					  | UIViewAutoresizing.FlexibleTopMargin
					  | UIViewAutoresizing.FlexibleHeight
					  | UIViewAutoresizing.FlexibleBottomMargin
		};
		
		MediaManagerDelegate.FullScreenChanged += OnFullScreenChanged;
	}

	public void StartSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		DispatchQueue.MainQueue.DispatchAsync(() => playerViewController.View?.AddSubview(subtitleLabel));
		playerObserver = player?.AddPeriodicTimeObserver(CMTime.FromSeconds(1, 1), null, (time) =>
		{
			TimeSpan currentPlaybackTime = TimeSpan.FromSeconds(time.Seconds);
			subtitleLabel.Frame = viewController is not null ? CalculateSubtitleFrame(viewController) : CalculateSubtitleFrame(playerViewController);
			DispatchQueue.MainQueue.DispatchAsync(() => UpdateSubtitle(currentPlaybackTime));
		});
	}

	public void StopSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(Cues);
		subtitleLabel.Text = string.Empty;
		Cues.Clear();
		subtitleLabel.BackgroundColor = clearBackgroundColor;
		DispatchQueue.MainQueue.DispatchAsync(() => subtitleLabel.RemoveFromSuperview());
	}
	void UpdateSubtitle(TimeSpan currentPlaybackTime)
	{
		ArgumentNullException.ThrowIfNull(Cues);
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		ArgumentNullException.ThrowIfNull(MediaElement);
		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl))
		{
			return;
		}

		foreach (var cue in Cues)
		{
			if (currentPlaybackTime >= cue.StartTime && currentPlaybackTime <= cue.EndTime)
			{
				subtitleLabel.Text = cue.Text;
				subtitleLabel.Font = UIFont.FromName(name: new Core.FontExtensions.FontFamily(MediaElement.SubtitleFont).MacIOS, size: (float)MediaElement.SubtitleFontSize) ?? UIFont.SystemFontOfSize(16);
				subtitleLabel.BackgroundColor = subtitleBackgroundColor;
				break;
			}
			else
			{
				subtitleLabel.Text = "";
				subtitleLabel.BackgroundColor = clearBackgroundColor;
			}
		}
	}

	static CGRect CalculateSubtitleFrame(UIViewController uIViewController)
	{ 
		if(uIViewController is null || uIViewController.View is null)
		{
			return CGRect.Empty;
		}
		return new CGRect(0, uIViewController.View.Bounds.Height - 60, uIViewController.View.Bounds.Width, 50);
	}

	void OnFullScreenChanged(object? sender, FullScreenEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(MediaElement);
		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl))
		{
			return;
		}
		subtitleLabel.Text = string.Empty;
		switch (e.isFullScreen)
		{
			case true:
				viewController = WindowStateManager.Default.GetCurrentUIViewController() ?? throw new ArgumentException(nameof(viewController));
				ArgumentNullException.ThrowIfNull(viewController.View);
				subtitleLabel.Frame = CalculateSubtitleFrame(viewController);
				DispatchQueue.MainQueue.DispatchAsync(() => { subtitleLabel.RemoveFromSuperview(); viewController?.View?.AddSubview(subtitleLabel); });
				break;
			case false:
				subtitleLabel.Frame = CalculateSubtitleFrame(playerViewController);
				DispatchQueue.MainQueue.DispatchAsync(() => { subtitleLabel.RemoveFromSuperview(); playerViewController.View?.AddSubview(subtitleLabel); });
				viewController = null;
				break;
		}
	}

	~SubtitleExtensions()
	{
		MediaManagerDelegate.FullScreenChanged -= OnFullScreenChanged;
		if(playerObserver is not null && player is not null)
		{
			player.RemoveTimeObserver(playerObserver);
		}
	}
}

