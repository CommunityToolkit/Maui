// Works if PR# 1873 merged
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Com.Google.Android.Exoplayer2.UI;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;

namespace CommunityToolkit.Maui.Extensions;
/// <summary>
/// A class that provides subtitle support for a video player.
/// </summary>
public partial class SubtitleExtensions : CoordinatorLayout
{
	bool disposedValue;
	bool isFullScreen = false;
	
	readonly HttpClient httpClient;
	readonly IDispatcher dispatcher;
	readonly RelativeLayout.LayoutParams? textBlockLayout;
	readonly StyledPlayerView styledPlayerView;
	readonly TextView textBlock;

	IMediaElement? mediaElement;
	List<SubtitleCue> cues;
	System.Timers.Timer? timer;

	/// <summary>
	/// The SubtitleExtensions class provides a way to display subtitles on a video player.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="styledPlayerView"></param>
	public SubtitleExtensions(Context context, StyledPlayerView styledPlayerView, IDispatcher dispatcher) : base(context)
	{
		httpClient = new HttpClient();
		this.dispatcher = dispatcher;
		this.styledPlayerView = styledPlayerView;
		cues = [];

		textBlockLayout = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
		textBlockLayout.AddRule(LayoutRules.AlignParentBottom);
		textBlockLayout.AddRule(LayoutRules.CenterHorizontal);
		textBlock = new(Platform.AppContext)
		{
			Text = string.Empty,
			HorizontalScrollBarEnabled = false,
			VerticalScrollBarEnabled = false,
			TextAlignment = Android.Views.TextAlignment.Center,
			Visibility = Android.Views.ViewStates.Gone,
			LayoutParameters = textBlockLayout
		};
		textBlock.SetBackgroundColor(Android.Graphics.Color.Argb(150, 0, 0, 0));
		textBlock.SetPaddingRelative(10, 10, 10, 10);
		textBlock.SetTextColor(Android.Graphics.Color.White);
		
		MauiMediaElement.WindowsChanged += MauiMediaElement_WindowsChanged;
	}

	void MauiMediaElement_WindowsChanged(object? sender, WindowsEventArgs e)
	{
		if (e.data is not ViewGroup viewGroup || styledPlayerView.Parent is not ViewGroup parent || string.IsNullOrEmpty(mediaElement?.SubtitleUrl))
		{
			return;
		}
		switch(isFullScreen)
		{
			case true:
				viewGroup.RemoveView(textBlock);
				parent.AddView(textBlock);
				isFullScreen = false;
				break;
			case false:
				parent.RemoveView(textBlock);
				viewGroup.AddView(textBlock);
				isFullScreen = true;
				break;
		}
	}
	
	/// <summary>
	/// Loads the subtitles from the provided URL.
	/// </summary>
	/// <param name="mediaElement"></param>
	public async Task LoadSubtitles(IMediaElement mediaElement)
	{
		this.mediaElement = mediaElement;
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
		if(textBlock is null || styledPlayerView.Parent is not ViewGroup parent)
		{
			return;
		}
		dispatcher.Dispatch(() => parent.AddView(textBlock));
		timer = new System.Timers.Timer(1000);
		timer.Elapsed += Timer_Elapsed;
		timer.Start();
	}

	void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
	{
		if (mediaElement?.Position is null || textBlock is null || cues.Count == 0)
		{
			return;
		}

		var cue = cues.Find(c => c.StartTime <= mediaElement.Position && c.EndTime >= mediaElement.Position);
		dispatcher.Dispatch(() =>
		{
			if (cue is not null)
			{
				textBlock.FontFeatureSettings = !string.IsNullOrEmpty(mediaElement.SubtitleFont) ? mediaElement.SubtitleFont : default;
				textBlock.Text = cue.Text;
				textBlock.TextSize = (float)mediaElement.SubtitleFontSize;
				textBlock.Visibility = Android.Views.ViewStates.Visible;
			}
			else
			{
				textBlock.Text = string.Empty;
				textBlock.Visibility = Android.Views.ViewStates.Gone;
			}
		});
	}

	/// <summary>
	/// Stops the subtitle timer.
	/// </summary>
	public void StopSubtitleDisplay()
	{
		if (timer is null)
		{
			return;
		}
		if(styledPlayerView.Parent is ViewGroup parent)
		{
			dispatcher.Dispatch(() =>
			{
				parent.RemoveView(textBlock);
			});
		}
		textBlock.Text = string.Empty;
		timer.Stop();
		timer.Elapsed -= Timer_Elapsed;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (!disposedValue)
		{
			if (disposing)
			{
				timer?.Stop();
				if(timer is not null)
				{
					timer.Elapsed -= Timer_Elapsed;
				}
				httpClient?.Dispose();
				timer?.Dispose();
			}
			timer = null;
			disposedValue = true;
		}
	}
}
