using CommunityToolkit.Maui.Core;
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
	UILabel subtitleLabel;
	static readonly UIColor subtitleBackgroundColor = UIColor.FromRGBA(0, 0, 0, 128);
	static readonly UIColor clearBackgroundColor = UIColor.FromRGBA(0, 0, 0, 0);
	NSObject? playerObserver;
	UIViewController? viewController;
	List<SubtitleCue>? cues;

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
			Font = UIFont.SystemFontOfSize(12),
			Text = string.Empty,
			Lines = 0,
			LineBreakMode = UILineBreakMode.WordWrap,
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth
					  | UIViewAutoresizing.FlexibleTopMargin
					  | UIViewAutoresizing.FlexibleHeight
					  | UIViewAutoresizing.FlexibleBottomMargin
		};
		MediaManager.FullScreenEvents.WindowsChanged += OnFullScreenChanged;
	}

	public void StartSubtitleDisplay()
	{
		cues = Document?.Cues;
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
		ArgumentNullException.ThrowIfNull(cues);
		subtitleLabel.Text = string.Empty;
		cues.Clear();
		subtitleLabel.BackgroundColor = clearBackgroundColor;
		DispatchQueue.MainQueue.DispatchAsync(() => subtitleLabel.RemoveFromSuperview());
	}

	void UpdateSubtitle(TimeSpan currentPlaybackTime)
	{
		ArgumentNullException.ThrowIfNull(cues);
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		ArgumentNullException.ThrowIfNull(MediaElement);
		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl))
		{
			return;
		}

		var currentCue = cues.Find(cue => currentPlaybackTime >= cue.StartTime && currentPlaybackTime <= cue.EndTime);

		if (currentCue is not null)
		{
			DisplayCue(currentCue);
		}
		else
		{
			subtitleLabel.Text = string.Empty;
			subtitleLabel.BackgroundColor = clearBackgroundColor;
		}
	}

	void DisplayCue(SubtitleCue cue)
	{
		if (cue.ParsedCueText is null)
		{
			return;
		}

		var attributedString = new NSMutableAttributedString();
		ProcessCueText(attributedString, cue.ParsedCueText);
		subtitleLabel.AttributedText = attributedString;

		ApplyStyles(cue);
		subtitleLabel.BackgroundColor = subtitleBackgroundColor;
	}

	void ProcessCueText(NSMutableAttributedString attributedString, SubtitleNode node)
	{
		foreach (var child in node.Children)
		{
			if (child.NodeType == "text")
			{
				string? text = child.TextContent;
				if (!string.IsNullOrEmpty(text))
				{
					var range = new NSRange(attributedString.Length, text.Length);
					attributedString.Append(new NSAttributedString(text));
					ApplyStyleToRange(attributedString, child.NodeType, range);
				}
			}
			else if (child.NodeType is not null)
			{
				var startLength = attributedString.Length;
				ProcessCueText(attributedString, child);
				var range = new NSRange(startLength, attributedString.Length - startLength);
				ApplyStyleToRange(attributedString, child.NodeType, range);
			}
		}
	}

	void ApplyStyleToRange(NSMutableAttributedString attributedString, string nodeType, NSRange range)
	{
		ArgumentNullException.ThrowIfNull(MediaElement);
		switch (nodeType.ToLower())
		{
			case "b":
				attributedString.AddAttribute(UIStringAttributeKey.Font, UIFont.BoldSystemFontOfSize((float)MediaElement.SubtitleFontSize), range);
				break;
			case "i":
				attributedString.AddAttribute(UIStringAttributeKey.Font, UIFont.ItalicSystemFontOfSize((float)MediaElement.SubtitleFontSize), range);
				break;
			case "u":
				attributedString.AddAttribute(UIStringAttributeKey.UnderlineStyle, NSNumber.FromInt32((int)NSUnderlineStyle.Single), range);
				break;
			case "v":
				attributedString.AddAttribute(UIStringAttributeKey.ForegroundColor, UIColor.Yellow, range);
				break;
		}
	}

	void ApplyStyles(SubtitleCue cue)
	{
		ArgumentNullException.ThrowIfNull(MediaElement);
		ArgumentNullException.ThrowIfNull(playerViewController.View);
		subtitleLabel.TextAlignment = GetTextAlignment(cue.Align);

		var font = UIFont.FromName(new Core.FontExtensions.FontFamily(MediaElement.SubtitleFont).MacIOS, (float)MediaElement.SubtitleFontSize) ?? UIFont.SystemFontOfSize((float)MediaElement.SubtitleFontSize);
		subtitleLabel.Font = font;

		if (!string.IsNullOrEmpty(cue.Position))
		{
			var parts = cue.Position.Split(',');
			if (parts.Length > 0 && float.TryParse(parts[0].TrimEnd('%'), out float horizontalPosition))
			{
				subtitleLabel.Frame = new CGRect(horizontalPosition * playerViewController.View.Bounds.Width / 100, subtitleLabel.Frame.Y, subtitleLabel.Frame.Width, subtitleLabel.Frame.Height);
			}
		}

		if (!string.IsNullOrEmpty(cue.Line) && float.TryParse(cue.Line.TrimEnd('%'), out float verticalPosition))
		{
			subtitleLabel.Frame = new CGRect(subtitleLabel.Frame.X, verticalPosition * playerViewController.View.Bounds.Height / 100, subtitleLabel.Frame.Width, subtitleLabel.Frame.Height);
		}

		if (cue.Vertical is not null)
		{
			ApplyVerticalWriting(cue.Vertical);
		}
	}

	static UITextAlignment GetTextAlignment(string align)
	{
		return align?.ToLower() switch
		{
			"left" => UITextAlignment.Left,
			"right" => UITextAlignment.Right,
			"center" => UITextAlignment.Center,
			_ => UITextAlignment.Center,
		};
	}

	void ApplyVerticalWriting(string vertical)
	{
		if (vertical == "rl" || vertical == "lr")
		{
			subtitleLabel.Transform = CGAffineTransform.MakeRotation(vertical == "rl" ? (float)Math.PI / 2 : -(float)Math.PI / 2);
		}
		else
		{
			subtitleLabel.Transform = CGAffineTransform.MakeIdentity();
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

	void OnFullScreenChanged(object? sender, FullScreenStateChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(MediaElement);
		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl))
		{
			return;
		}
		subtitleLabel.Text = string.Empty;
		switch (e.NewState == MediaElementScreenState.FullScreen)
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
		MediaManager.FullScreenEvents.WindowsChanged -= OnFullScreenChanged;
		if(playerObserver is not null && player is not null)
		{
			player.RemoveTimeObserver(playerObserver);
		}
	}
}

