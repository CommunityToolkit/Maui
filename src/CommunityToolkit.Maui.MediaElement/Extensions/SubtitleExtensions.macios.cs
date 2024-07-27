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
		ArgumentNullException.ThrowIfNull(Cues);
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		if (playerViewController is null || string.IsNullOrEmpty(MediaElement?.SubtitleUrl))
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

		var fontSize = GetFontSize((float)MediaElement.SubtitleFontSize);
		subtitleLabel.Text = TextWrapper(text);
		subtitleLabel.Font = GetFontFamily(MediaElement.SubtitleFont, fontSize);
		subtitleLabel.BackgroundColor = subtitleBackgroundColor;
		var labelWidth = GetSubtileWidth(text);

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

	nfloat GetSubtileWidth(string? text)
	{
		var nsString = new NSString(subtitleLabel.Text);
		var attributes = new UIStringAttributes { Font = subtitleLabel.Font };
		var textSize = nsString.GetSizeUsingAttributes(attributes);
		 return textSize.Width + 5;
	}

	float GetFontSize(float fontSize)
	{
		ArgumentNullException.ThrowIfNull(MediaElement);
		#if IOS
		return fontSize;
		#else
		return screenState == MediaElementScreenState.FullScreen? (float)MediaElement.SubtitleFontSize * 1.5f : (float)MediaElement.SubtitleFontSize;
		#endif
	}

	static UIFont GetFontFamily(string fontFamily, float fontSize) =>  UIFont.FromName(new Core.FontExtensions.FontFamily(fontFamily).MacIOS, fontSize);

	static UIViewController GetCurrentUIViewController()
	{
		UIViewController? viewController = null;
#if IOS
		viewController = WindowStateManager.Default.GetCurrentUIViewController();
#else
		// Must use KeyWindow as it is the only one that will be available when the app is in full screen mode on macOS.
		// It is deprecated for use in MacOS apps, but is still available and the only choice for this scenario.
		viewController =  UIApplication.SharedApplication.KeyWindow?.RootViewController;
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
