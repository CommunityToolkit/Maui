using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2.UI;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using static Android.Views.ViewGroup;
using Activity = Android.App.Activity;

namespace CommunityToolkit.Maui.Extensions;

class SubtitleExtensions : Java.Lang.Object
{
	readonly IDispatcher dispatcher;
	readonly RelativeLayout.LayoutParams? subtitleLayout;
	readonly StyledPlayerView styledPlayerView;
	readonly CurrentPlatformActivity platform;
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
		ArgumentNullException.ThrowIfNull(Platform.CurrentActivity);
		this.dispatcher = dispatcher;
		this.styledPlayerView = styledPlayerView;
		if (Platform.CurrentActivity.Window?.DecorView is not ViewGroup decorView)
		{
			throw new InvalidOperationException("Platform.CurrentActivity.Window.DecorView is not a ViewGroup");
		}
		platform = new(Platform.CurrentActivity, decorView);
		cues = [];

		subtitleLayout = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
		subtitleLayout.AddRule(LayoutRules.AlignParentBottom);
		subtitleLayout.AddRule(LayoutRules.CenterHorizontal);

		InitializeTextBlock();
		MauiMediaElement.FullScreenChanged += OnFullScreenChanged;
	}

	/// <summary>
	/// Loads the subtitles from the provided URL.
	/// </summary>
	/// <param name="mediaElement"></param>
	public async Task LoadSubtitles(IMediaElement mediaElement)
	{
		this.mediaElement = mediaElement;
		cues.Clear();
		Parser parser;
		var content = await Parser.Content(mediaElement.SubtitleUrl);
		if (mediaElement.CustomParser is not null)
		{
			parser = new(mediaElement.CustomParser);
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
				subtitleView.Visibility = ViewStates.Visible;
			}
			else
			{
				subtitleView.Text = string.Empty;
				subtitleView.Visibility = ViewStates.Gone;
			}
		});
	}

	void InitializeTextBlock()
	{
		subtitleView = new(platform.currentActivity.ApplicationContext)
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

	readonly record struct CurrentPlatformActivity(Activity currentActivity, ViewGroup viewGroup)
	{
		public Activity CurrentActivity { get; init; } = currentActivity;
		public ViewGroup ViewGroup { get; init; } = viewGroup;
	}

	void OnFullScreenChanged(object? sender, FullScreenEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(subtitleView);

		// If the subtitle URL is empty do nothing
		if (string.IsNullOrEmpty(mediaElement?.SubtitleUrl))
		{
			return;
		}
		
		if (platform.viewGroup.Parent is not ViewGroup parent)
		{
			return;
		}

		switch (e.isFullScreen)
		{
			case true:
				platform.viewGroup.RemoveView(subtitleView);
				InitializeTextBlock();
				parent.AddView(subtitleView);
				break;
			case false:
				parent.RemoveView(subtitleView);
				InitializeTextBlock();
				platform.viewGroup.AddView(subtitleView);
				break;
		}
	}
}
