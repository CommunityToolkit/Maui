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
	static readonly HttpClient httpClient = new();
	readonly PlatformMediaElement player;
	readonly UIViewController playerViewController;
	readonly UILabel subtitleLabel;

	List<SubtitleCue> cues;
	IMediaElement? mediaElement;
	NSObject? playerObserver;
	UIViewController? viewController;

	/// <summary>
	/// The SubtitleExtensions class provides a way to display subtitles on a video player.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="playerViewController"></param>
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
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleBottomMargin
		};
		MediaManagerDelegate.WindowChanged += OnWindowStatusChanged;
	}

	/// <summary>
	/// Loads the subtitles from the provided URL.
	/// </summary>
	/// <param name="mediaElement"></param>
	public async Task LoadSubtitles(IMediaElement mediaElement)
	{
		this.mediaElement = mediaElement;
		cues.Clear();
		subtitleLabel.Font = UIFont.FromName(mediaElement.SubtitleFont, (float)mediaElement.SubtitleFontSize) ?? UIFont.SystemFontOfSize((float)mediaElement.SubtitleFontSize);
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
			switch(viewController)
			{
				case null:
					DispatchQueue.MainQueue.DispatchAsync(() => playerViewController.View?.AddSubview(subtitleLabel));
					break;
				default:
					DispatchQueue.MainQueue.DispatchAsync(() => viewController?.View?.AddSubview(subtitleLabel));
					break;
			}
			subtitleLabel.Frame = viewController is not null ? CalculateSubtitleFrame(viewController) : CalculateSubtitleFrame(playerViewController);
			DispatchQueue.MainQueue.DispatchAsync(() => UpdateSubtitle(currentPlaybackTime));
		});
	}

	/// <summary>
	/// Stops the subtitle display.
	/// </summary>
	public void StopSubtitleDisplay()
	{
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

	void OnWindowStatusChanged(object? sender, WindowsEventArgs e)
	{
		if (string.IsNullOrEmpty(mediaElement?.SubtitleUrl) || e.data is null)
		{
			return;
		}
		switch (e.data)
		{
			case true:
				viewController = WindowStateManager.Default.GetCurrentUIViewController() ?? throw new ArgumentException(nameof(viewController));
				ArgumentNullException.ThrowIfNull(viewController.View);
				subtitleLabel.Frame = CalculateSubtitleFrame(viewController);
				viewController.View.AddSubview(subtitleLabel);
				break;
			case false:
				subtitleLabel.Frame = CalculateSubtitleFrame(playerViewController);
				viewController = null;
				break;
		}
	}
}

