using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// A class that provides subtitle support for a video player.
/// </summary>
public partial class SubtitleExtensions : UIViewController
{
	readonly HttpClient httpClient;
	readonly UIViewController playerViewController;
	readonly PlatformMediaElement player;
	UILabel? subtitleLabel;
	List<SubtitleCue> cues;
	NSObject? playerObserver;
	IMediaElement? mediaElement;
	UIViewController? viewController;
	UIFont? font;

	/// <summary>
	/// The SubtitleExtensions class provides a way to display subtitles on a video player.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="playerViewController"></param>
	public SubtitleExtensions(PlatformMediaElement? player, UIViewController? playerViewController)
	{
		ArgumentNullException.ThrowIfNull(player);
		ArgumentNullException.ThrowIfNull(playerViewController?.View?.Bounds);
		this.playerViewController = playerViewController;
		this.player = player;
		MediaManagerDelegate.WindowsChanged += MauiMediaElement_WindowsChanged;
		cues = [];
		httpClient = new HttpClient();
		subtitleLabel = new UILabel
		{
			Frame = CalculateSubtitleFrame(playerViewController),
			TextColor = UIColor.White,
			TextAlignment = UITextAlignment.Center,
			Font = UIFont.SystemFontOfSize(16),
			Text = "",
			Lines = 0,
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleBottomMargin
		};
	}

	void MauiMediaElement_WindowsChanged(object? sender, WindowsEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(font);
		if (string.IsNullOrEmpty(mediaElement?.SubtitleUrl) || e.data is null)
		{
			return;
		}
		subtitleLabel = new UILabel
		{
			TextColor = UIColor.White,
			TextAlignment = UITextAlignment.Center,
			Font = font,
			Text = "",
			Lines = 0,
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleBottomMargin
		};
		switch (e.data.Equals(true))
		{
			case true:
				viewController = WindowStateManager.Default.GetCurrentUIViewController();
				ArgumentNullException.ThrowIfNull(viewController?.View);
				subtitleLabel.Frame = CalculateSubtitleFrame(viewController);
				viewController.View.AddSubview(subtitleLabel);
				break;
			case false:
				subtitleLabel.Frame = CalculateSubtitleFrame(playerViewController);
				viewController = null;
				break;
		}
	}

	/// <summary>
	/// Loads the subtitles from the provided URL.
	/// </summary>
	/// <param name="mediaElement"></param>
	public async Task LoadSubtitles(IMediaElement mediaElement)
	{
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		this.mediaElement = mediaElement;
		font = UIFont.FromName(mediaElement.SubtitleFont, (float)mediaElement.SubtitleFontSize) ?? UIFont.SystemFontOfSize((float)mediaElement.SubtitleFontSize);
		subtitleLabel.Font = font;
		string? vttContent;
		try
		{
			vttContent = await httpClient.GetStringAsync(mediaElement.SubtitleUrl);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError(ex.Message);
			return;
		}

		cues = mediaElement.SubtitleUrl switch
		{
			var url when url.EndsWith("srt") => SrtParser.ParseSrtContent(vttContent),
			var url when url.EndsWith("vtt") => VttParser.ParseVttContent(vttContent),
			_ => throw new NotSupportedException("Unsupported subtitle format"),
		};
	}

	/// <summary>
	/// Starts the subtitle display.
	/// </summary>
	public void StartSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		playerObserver = player?.AddPeriodicTimeObserver(CMTime.FromSeconds(1, 1), null, (time) =>
		{
			TimeSpan currentPlaybackTime = TimeSpan.FromSeconds(time.Seconds);
			if (viewController is not null)
			{
				DispatchQueue.MainQueue.DispatchAsync(() => viewController?.View?.AddSubview(subtitleLabel));
				subtitleLabel.Frame = CalculateSubtitleFrame(viewController);
			}
			else
			{
				DispatchQueue.MainQueue.DispatchAsync(() => playerViewController.View?.AddSubview(subtitleLabel));
				subtitleLabel.Frame = CalculateSubtitleFrame(playerViewController);
			}
			DispatchQueue.MainQueue.DispatchAsync(() => UpdateSubtitle(currentPlaybackTime));
		});
	}

	/// <summary>
	/// Stops the subtitle display.
	/// </summary>
	public void StopSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(player);
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		if (playerObserver is not null)
		{
			player.RemoveTimeObserver(playerObserver);
			playerObserver.Dispose();
			playerObserver = null;
			subtitleLabel.RemoveFromSuperview();
		}
	}
	void UpdateSubtitle(TimeSpan currentPlaybackTime)
	{
		ArgumentNullException.ThrowIfNull(subtitleLabel);
		foreach (var cue in cues)
		{
			if (currentPlaybackTime >= cue.StartTime && currentPlaybackTime <= cue.EndTime)
			{
				subtitleLabel.Text = cue.Text;
				subtitleLabel.BackgroundColor = UIColor.FromRGBA(0, 0, 0, 128);
				break;
			}
			else
			{
				subtitleLabel.Text = "";
				subtitleLabel.BackgroundColor = UIColor.FromRGBA(0, 0, 0, 0);
			}
		}
	}

	static CGRect CalculateSubtitleFrame(UIViewController uIViewController)
	{
		ArgumentNullException.ThrowIfNull(uIViewController?.View?.Bounds);
		return new CGRect(0, uIViewController.View.Bounds.Height - 60, uIViewController.View.Bounds.Width, 50);
	}
	
}

