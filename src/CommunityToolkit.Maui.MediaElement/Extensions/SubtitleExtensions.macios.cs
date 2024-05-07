using CommunityToolkit.Maui.Core;
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
	readonly PlatformMediaElement? player;
	readonly UILabel subtitleLabel;
	List<SubtitleCue> cues;
	NSObject? playerObserver;

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
		cues = [];
		httpClient = new HttpClient();
		subtitleLabel = new UILabel
		{
			Frame = CalculateSubtitleFrame(),
			TextColor = UIColor.White,
			TextAlignment = UITextAlignment.Center,
			Font = UIFont.SystemFontOfSize(16),
			Text = "",
			Lines = 0,
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleBottomMargin
		};
		playerViewController.View.AddSubview(subtitleLabel);
	}

	/// <summary>
	/// Loads the subtitles from the provided URL.
	/// </summary>
	/// <param name="mediaElement"></param>
	public async Task LoadSubtitles(IMediaElement mediaElement)
	{
		string vttContent = string.Empty;
		try
		{
			vttContent = await httpClient.GetStringAsync(mediaElement.SubtitleUrl).ConfigureAwait(true) ?? string.Empty;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError(ex.Message);
		}

		if (string.IsNullOrEmpty(vttContent))
		{
			System.Diagnostics.Trace.TraceError("VTT Subtitle Content is Empty");
			return;
		}
		if (mediaElement.SubtitleUrl.EndsWith("srt"))
		{
			cues = SrtParser.ParseSrtContent(vttContent);
		}
		else if (mediaElement.SubtitleUrl.EndsWith("vtt"))
		{
			cues = VttParser.ParseVttContent(vttContent);
		}
	}

	/// <summary>
	/// Starts the subtitle display.
	/// </summary>
	public void StartSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(player);
		if(cues?.Count == 0)
		{
			return;
		}
		if (playerObserver is not null)
		{
			StopSubtitleDisplay();
		}
		playerObserver = player.AddPeriodicTimeObserver(CMTime.FromSeconds(1, 1), null, (time) =>
		{
			TimeSpan currentPlaybackTime = TimeSpan.FromSeconds(time.Seconds);
			ArgumentNullException.ThrowIfNull(subtitleLabel);
			DispatchQueue.MainQueue.DispatchAsync(() => UpdateSubtitle(currentPlaybackTime));
		});
	}

	/// <summary>
	/// Stops the subtitle display.
	/// </summary>
	public void StopSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(player);
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
		ArgumentNullException.ThrowIfNull(playerViewController.View);
		subtitleLabel.RemoveFromSuperview();
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
		subtitleLabel.Frame = CalculateSubtitleFrame();
		playerViewController.View.AddSubview(subtitleLabel);
	}
	CGRect CalculateSubtitleFrame()
	{
		ArgumentNullException.ThrowIfNull(playerViewController?.View?.Bounds);
		return new CGRect(0, playerViewController.View.Bounds.Height - 60, playerViewController.View.Bounds.Width, 50);
	}
	
}

