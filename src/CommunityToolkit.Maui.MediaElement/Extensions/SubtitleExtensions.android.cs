using Android.Content.Res;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2.UI;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using static Android.Views.ViewGroup;
using Activity = Android.App.Activity;

namespace CommunityToolkit.Maui.Extensions;

partial class SubtitleExtensions : IDisposable
{
	bool disposedValue;

	static readonly HttpClient httpClient = new();
	readonly IDispatcher dispatcher;
	readonly RelativeLayout.LayoutParams? textBlockLayout;
	readonly StyledPlayerView styledPlayerView;

	List<SubtitleCue> cues;
	IMediaElement? mediaElement;
	TextView? textBlock;
	System.Timers.Timer? timer;

	/// <summary>
	/// The SubtitleExtensions class provides a way to display subtitles on a video player.
	/// </summary>
	/// <param name="styledPlayerView"></param>
	public SubtitleExtensions(StyledPlayerView styledPlayerView, IDispatcher dispatcher)
	{
		this.dispatcher = dispatcher;
		this.styledPlayerView = styledPlayerView;
		cues = [];

		textBlockLayout = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
		textBlockLayout.AddRule(LayoutRules.AlignParentBottom);
		textBlockLayout.AddRule(LayoutRules.CenterHorizontal);
		InitializeTextBlock();

		MauiMediaElement.WindowsChanged += MauiMediaElement_WindowsChanged;
	}
	
	/// <summary>
	/// Loads the subtitles from the provided URL.
	/// </summary>
	/// <param name="mediaElement"></param>
	public async Task LoadSubtitles(IMediaElement mediaElement)
	{
		this.mediaElement = mediaElement;
		cues.Clear();
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
		ArgumentNullException.ThrowIfNull(textBlock);
		if(styledPlayerView.Parent is not ViewGroup parent)
		{
			System.Diagnostics.Trace.TraceError("StyledPlayerView parent is not a ViewGroup");
			return;
		}
		dispatcher.Dispatch(() => parent.AddView(textBlock));
		timer = new System.Timers.Timer(1000);
		timer.Elapsed += Timer_Elapsed;
		timer.Start();
	}

	/// <summary>
	/// Stops the subtitle timer.
	/// </summary>
	public void StopSubtitleDisplay()
	{
		if (timer is null || textBlock is null)
		{
			return;
		}
		if (styledPlayerView.Parent is ViewGroup parent)
		{
			dispatcher.Dispatch(() => parent.RemoveView(textBlock));
		}
		textBlock.Text = string.Empty;
		timer.Stop();
		timer.Elapsed -= Timer_Elapsed;
	}

	void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(textBlock);
		ArgumentNullException.ThrowIfNull(mediaElement);
		if (cues.Count == 0)
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

	void InitializeTextBlock()
	{
		var (currentActivity, _, _, _) = VerifyAndRetrieveCurrentWindowResources();
		var activity = currentActivity;
		textBlock = new(activity.ApplicationContext)
		{
			Text = string.Empty,
			HorizontalScrollBarEnabled = false,
			VerticalScrollBarEnabled = false,
			TextAlignment = Android.Views.TextAlignment.Center,
			Visibility = Android.Views.ViewStates.Gone,
			LayoutParameters = textBlockLayout
		};
		textBlock.SetBackgroundColor(Android.Graphics.Color.Argb(150, 0, 0, 0));
		textBlock.SetTextColor(Android.Graphics.Color.White);
		textBlock.SetPaddingRelative(10, 10, 10, 20);
	}

	static (Activity CurrentActivity, Android.Views.Window CurrentWindow, Resources CurrentWindowResources, Configuration CurrentWindowConfiguration) VerifyAndRetrieveCurrentWindowResources()
	{
		// Ensure current activity and window are available
		if (Platform.CurrentActivity is not Activity currentActivity)
		{
			throw new InvalidOperationException("CurrentActivity cannot be null when the FullScreen button is tapped");
		}
		if (currentActivity.Window is not Android.Views.Window currentWindow)
		{
			throw new InvalidOperationException("CurrentActivity Window cannot be null when the FullScreen button is tapped");
		}

		if (currentActivity.Resources is not Resources currentResources)
		{
			throw new InvalidOperationException("CurrentActivity Resources cannot be null when the FullScreen button is tapped");
		}

		if (currentResources.Configuration is not Configuration configuration)
		{
			throw new InvalidOperationException("CurrentActivity Configuration cannot be null when the FullScreen button is tapped");
		}

		return (currentActivity, currentWindow, currentResources, configuration);
	}

	void MauiMediaElement_WindowsChanged(object? sender, WindowsEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(textBlock);

		// If the subtitle URL is empty do nothing
		if (string.IsNullOrEmpty(mediaElement?.SubtitleUrl))
		{
			return;
		}
		var (_, currentWindow, _, _) = VerifyAndRetrieveCurrentWindowResources();
		if (currentWindow?.DecorView is not ViewGroup viewGroup)
		{
			return;
		}
		if (viewGroup.Parent is not ViewGroup parent)
		{
			return;
		}
		switch (e.data)
		{
			case true:
				viewGroup.RemoveView(textBlock);
				InitializeTextBlock();
				parent.AddView(textBlock);
				break;
			case false:
				parent.RemoveView(textBlock);
				InitializeTextBlock();
				viewGroup.AddView(textBlock);
				break;
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				httpClient.Dispose();
				timer?.Dispose();
				textBlock?.Dispose();
			}

			timer = null;
			textBlock = null;
			disposedValue = true;
		}
	}

	~SubtitleExtensions()
	{
	     Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
