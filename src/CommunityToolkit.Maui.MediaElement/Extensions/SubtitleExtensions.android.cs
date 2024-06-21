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
	readonly RelativeLayout.LayoutParams? subtitleLayout;
	readonly StyledPlayerView styledPlayerView;

	List<SubtitleCue> cues;
	IMediaElement? mediaElement;
	TextView? subtitleView;
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

		subtitleLayout = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
		subtitleLayout.AddRule(LayoutRules.AlignParentBottom);
		subtitleLayout.AddRule(LayoutRules.CenterHorizontal);
		InitializeTextBlock();

		MauiMediaElement.WindowChanged += OnWindowStatusChanged;
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
		ArgumentNullException.ThrowIfNull(subtitleView);
		if(styledPlayerView.Parent is not ViewGroup parent)
		{
			System.Diagnostics.Trace.TraceError("StyledPlayerView parent is not a ViewGroup");
			return;
		}
		dispatcher.Dispatch(() => parent.AddView(subtitleView));
		timer = new System.Timers.Timer(1000);
		timer.Elapsed += UpdateSubtitle;
		timer.Start();
	}

	/// <summary>
	/// Stops the subtitle timer.
	/// </summary>
	public void StopSubtitleDisplay()
	{
		if (timer is null || subtitleView is null)
		{
			return;
		}
		if (styledPlayerView.Parent is ViewGroup parent)
		{
			dispatcher.Dispatch(() => parent.RemoveView(subtitleView));
		}
		subtitleView.Text = string.Empty;
		timer.Stop();
		timer.Elapsed -= UpdateSubtitle;
	}

	void UpdateSubtitle(object? sender, System.Timers.ElapsedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(subtitleView);
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
				subtitleView.FontFeatureSettings = !string.IsNullOrEmpty(mediaElement.SubtitleFont) ? mediaElement.SubtitleFont : default;
				subtitleView.Text = cue.Text;
				subtitleView.TextSize = (float)mediaElement.SubtitleFontSize;
				subtitleView.Visibility = Android.Views.ViewStates.Visible;
			}
			else
			{
				subtitleView.Text = string.Empty;
				subtitleView.Visibility = Android.Views.ViewStates.Gone;
			}
		});
	}

	void InitializeTextBlock()
	{
		var (currentActivity, _, _, _) = VerifyAndRetrieveCurrentWindowResources();
		var activity = currentActivity;
		subtitleView = new(activity.ApplicationContext)
		{
			Text = string.Empty,
			HorizontalScrollBarEnabled = false,
			VerticalScrollBarEnabled = false,
			TextAlignment = Android.Views.TextAlignment.Center,
			Visibility = Android.Views.ViewStates.Gone,
			LayoutParameters = subtitleLayout
		};
		subtitleView.SetBackgroundColor(Android.Graphics.Color.Argb(150, 0, 0, 0));
		subtitleView.SetTextColor(Android.Graphics.Color.White);
		subtitleView.SetPaddingRelative(10, 10, 10, 20);
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

	void OnWindowStatusChanged(object? sender, WindowsEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(subtitleView);

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
				viewGroup.RemoveView(subtitleView);
				InitializeTextBlock();
				parent.AddView(subtitleView);
				break;
			case false:
				parent.RemoveView(subtitleView);
				InitializeTextBlock();
				viewGroup.AddView(subtitleView);
				break;
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (timer is not null)
			{
				timer.Stop();
				timer.Elapsed -= UpdateSubtitle;
			}

			if (disposing)
			{
				httpClient.Dispose();
				timer?.Dispose();
				subtitleView?.Dispose();
			}

			timer = null;
			subtitleView = null;
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
