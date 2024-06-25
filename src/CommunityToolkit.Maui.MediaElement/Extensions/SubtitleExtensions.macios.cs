using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Extensions;

class SubtitleExtensions : UIViewController
{
	readonly PlatformMediaElement player;
	readonly UIViewController playerViewController;
	readonly UILabel subtitleLabel;

	static readonly UIColor subtitleBackgroundColor = UIColor.FromRGBA(0, 0, 0, 128);
	static readonly UIColor clearBackgroundColor = UIColor.FromRGBA(0, 0, 0, 0);

	List<SubtitleCue> cues;
	IMediaElement? mediaElement;
	NSObject? playerObserver;
	UIViewController? viewController;

	public SubtitleExtensions(PlatformMediaElement player, UIViewController playerViewController)
	{
		this.playerViewController = playerViewController;
		this.player = player;
		cues = [];
		subtitleLabel = new UILabel
		{
			Frame = CalculateSubtitleFrame(playerViewController),
			TextColor = UIColor.White,
			TextAlignment = UITextAlignment.Center,
			Font = UIFont.SystemFontOfSize(16),
			Text = "",
			Lines = 0,
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth
					  | UIViewAutoresizing.FlexibleTopMargin
					  | UIViewAutoresizing.FlexibleHeight
					  | UIViewAutoresizing.FlexibleBottomMargin
		};
		
		MediaManagerDelegate.FullScreenChanged += OnFullScreenChanged;
	}

	public async Task LoadSubtitles(IMediaElement mediaElement)
	{
		this.mediaElement = mediaElement;
		cues.Clear();
		SubtitleParser parser;
		var content = await SubtitleParser.Content(mediaElement.SubtitleUrl);
		if (mediaElement.CustomSubtitleParser is not null)
		{
			parser = new(mediaElement.CustomSubtitleParser);
			cues = parser.ParseContent(content);
			return;
		}
		switch (mediaElement.SubtitleUrl)
		{
			case var url when url.EndsWith("srt"):
				parser = new(new SrtParser());
				cues = parser.ParseContent(content);
				break;
			case var url when url.EndsWith("vtt"):
				parser = new(new VttParser());
				cues = parser.ParseContent(content);
				break;
			default:
				System.Diagnostics.Trace.TraceError("Unsupported Subtitle file.");
				return;
		}
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
		subtitleLabel.Text = string.Empty;
		subtitleLabel.BackgroundColor = clearBackgroundColor;
		DispatchQueue.MainQueue.DispatchAsync(() => subtitleLabel.RemoveFromSuperview());

		if (playerObserver is null)
		{
			return;
		}
		player.RemoveTimeObserver(playerObserver);
	}
	void UpdateSubtitle(TimeSpan currentPlaybackTime)
	{
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		ArgumentNullException.ThrowIfNull(mediaElement);
		foreach (var cue in cues)
		{
			if (currentPlaybackTime >= cue.StartTime && currentPlaybackTime <= cue.EndTime)
			{
				subtitleLabel.Text = cue.Text;
				subtitleLabel.Font = UIFont.FromName(name: DeviceFontSpecs.OutputDeviceSpecifications(mediaElement.SubtitleFont).iOSFont,size: (float)mediaElement.SubtitleFontSize) ?? UIFont.SystemFontOfSize(16);
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
		if (string.IsNullOrEmpty(mediaElement?.SubtitleUrl))
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
}

