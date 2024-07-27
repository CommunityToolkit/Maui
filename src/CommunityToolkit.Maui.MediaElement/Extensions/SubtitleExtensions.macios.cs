using System.Text;
using System.Text.RegularExpressions;
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
	MediaElementScreenState screenState;

	public SubtitleExtensions(PlatformMediaElement player, UIViewController playerViewController)
	{
		this.playerViewController = playerViewController;
		this.player = player;
		screenState = MediaElementScreenState.Default;
		Cues = [];

		subtitleLabel = new UILabel
		{
			Frame = CalculateSubtitleFrame(playerViewController, 100),
			TextColor = UIColor.White,
			TextAlignment = UITextAlignment.Center,
			Font = UIFont.SystemFontOfSize(12),
			Text = string.Empty,
			BackgroundColor = clearBackgroundColor,
			Lines = 0,
			LineBreakMode = UILineBreakMode.WordWrap,
		};
		MediaManager.FullScreenEvents.WindowsChanged += OnFullScreenChanged;
	}

	public void StartSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		DispatchQueue.MainQueue.DispatchAsync(() => playerViewController.View?.AddSubview(subtitleLabel));

		playerObserver = player?.AddPeriodicTimeObserver(CMTime.FromSeconds(1, 1), null, (time) =>
		{
			DispatchQueue.MainQueue.DispatchAsync(() => UpdateSubtitle());
		});
	}

	public void StopSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(Cues);
		subtitleLabel.Text = string.Empty;
		Cues.Clear();
		subtitleLabel.BackgroundColor = clearBackgroundColor;
		DispatchQueue.MainQueue.DispatchAsync(() => subtitleLabel.RemoveFromSuperview());
		playerObserver?.Dispose();
	}

	void UpdateSubtitle()
	{
		if (playerViewController is null)
		{
			return;
		}

		ArgumentNullException.ThrowIfNull(Cues);
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		ArgumentNullException.ThrowIfNull(MediaElement);

		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl))
		{
			return;
		}

		var cue = Cues.Find(c => c.StartTime <= MediaElement.Position && c.EndTime >= MediaElement.Position);
		if (cue is not null)
		{
			SetText(cue.Text);
		}
		else
		{
			subtitleLabel.Text = string.Empty;
			subtitleLabel.BackgroundColor = clearBackgroundColor;
		}
	}
	void SetText(string? text)
	{
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		ArgumentNullException.ThrowIfNull(MediaElement);
		subtitleLabel.Text = TextWrapper(text ?? string.Empty);
		subtitleLabel.Font = UIFont.FromName(new Core.FontExtensions.FontFamily(MediaElement.SubtitleFont).MacIOS, (float)MediaElement.SubtitleFontSize) ?? UIFont.SystemFontOfSize(16);
		subtitleLabel.BackgroundColor = subtitleBackgroundColor;

		var nsString = new NSString(subtitleLabel.Text);
		var attributes = new UIStringAttributes { Font = subtitleLabel.Font };
		var textSize = nsString.GetSizeUsingAttributes(attributes);
		var labelWidth = textSize.Width + 5;

		switch (screenState)
		{
			case MediaElementScreenState.FullScreen:
				var viewController = GetCurrentUIViewController();
				ArgumentNullException.ThrowIfNull(viewController);
				subtitleLabel.Frame = CalculateSubtitleFrame(viewController, labelWidth);
				break;
			case MediaElementScreenState.Default:
				subtitleLabel.Frame = CalculateSubtitleFrame(playerViewController, labelWidth);
				break;
		}
	}

	UIViewController GetCurrentUIViewController()
	{
		UIViewController? viewController = null;

		// Must use KeyWindow as it is the only one that will be available when the app is in full screen mode on macOS.
		// It is deprecated for use in MacOS apps, but is still available and the only choice for this scenario.
		#if MACCATALYST
				viewController =  UIApplication.SharedApplication.KeyWindow?.RootViewController ?? Platform.GetCurrentUIViewController();
		#endif
		#if IOS
			    viewController = WindowStateManager.Default.GetCurrentUIViewController();
		#endif
		ArgumentNullException.ThrowIfNull(viewController);
		return viewController;
	}

	static string TextWrapper(string input)
	{
		Regex wordRegex = MatchWorksRegex();
		MatchCollection words = wordRegex.Matches(input);

		StringBuilder wrappedTextBuilder = new();
		int currentLineLength = 0;
		int lineNumber = 1;

		foreach (var matchValue in
		from Match match in words
		let matchValue = match.Value
		select matchValue)
		{
			if (currentLineLength + matchValue.Length > 60)
			{
				wrappedTextBuilder.AppendLine();
				lineNumber++;
				currentLineLength = 0;
			}

			if (currentLineLength > 0)
			{
				wrappedTextBuilder.Append(' ');
			}

			wrappedTextBuilder.Append(matchValue);
			currentLineLength += matchValue.Length + 1;
		}

		return wrappedTextBuilder.ToString();
	}

	static CGRect CalculateSubtitleFrame(UIViewController uIViewController, nfloat labelWidth)
	{
		if (uIViewController is null || uIViewController.View is null)
		{
			return CGRect.Empty;
		}
		var viewWidth = uIViewController.View.Bounds.Width;
		var viewHeight = uIViewController.View.Bounds.Height;
		var x = (viewWidth - labelWidth) / 2;
		return new CGRect(x, viewHeight - 80, labelWidth, 50);
	}

	void OnFullScreenChanged(object? sender, FullScreenStateChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(MediaElement);
		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl))
		{
			return;
		}

		DispatchQueue.MainQueue.DispatchAsync(subtitleLabel.RemoveFromSuperview);
		switch (e.NewState == MediaElementScreenState.FullScreen)
		{
			case true:
				var viewController =  GetCurrentUIViewController();
				screenState = MediaElementScreenState.FullScreen;
				ArgumentNullException.ThrowIfNull(viewController.View);
				DispatchQueue.MainQueue.DispatchAsync(() => viewController?.View?.Add(subtitleLabel)); 
				break;
			case false:
				screenState = MediaElementScreenState.Default;
				DispatchQueue.MainQueue.DispatchAsync(() => playerViewController.View?.AddSubview(subtitleLabel));
				break;
		}
		DispatchQueue.MainQueue.DispatchAsync(UpdateSubtitle);
	}

	~SubtitleExtensions()
	{
		MediaManager.FullScreenEvents.WindowsChanged -= OnFullScreenChanged;
		playerObserver?.Dispose();
	}

	[GeneratedRegex(@"\b\w+\b")]
	private static partial Regex MatchWorksRegex();
}
