﻿using Android.Content;
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
	readonly TextView textBlock;
	readonly StyledPlayerView styledPlayerView;
	readonly IDispatcher dispatcher;

	System.Timers.Timer? timer;
	List<SubtitleCue> cues;
	
	IMediaElement? mediaElement;
	
	RelativeLayout? relativeLayout;
	RelativeLayout? fullScreenLayout;
	
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
		textBlock = new(Platform.AppContext)
		{
			Text = string.Empty,
			HorizontalScrollBarEnabled = false,
			VerticalScrollBarEnabled = false,
			TextSize = 16,
			TextAlignment = Android.Views.TextAlignment.Center,
			Visibility = Android.Views.ViewStates.Gone,
		};
		textBlock.SetBackgroundColor(Android.Graphics.Color.Argb(150, 0, 0, 0));
		textBlock.SetPadding(10, 10, 10, 10);
		textBlock.SetTextColor(Android.Graphics.Color.White);

		MauiMediaElement.WindowsChanged += MauiMediaElement_WindowsChanged;
	}

	void MauiMediaElement_WindowsChanged(object? sender, WindowsEventArgs e)
	{
		if (e.data is not ViewGroup viewGroup || styledPlayerView.Parent is not ViewGroup parent || string.IsNullOrEmpty(mediaElement?.SubtitleUrl))
		{
			return;
		}
		if (isFullScreen)
		{
			relativeLayout = new(Platform.AppContext)
			{
				LayoutParameters = new CoordinatorLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
				{
					Gravity = (int)GravityFlags.Bottom,
					BottomMargin = 10,
				}
			};
			viewGroup.RemoveView(fullScreenLayout);
			fullScreenLayout?.RemoveView(textBlock);
			relativeLayout.AddView(textBlock);
			parent.AddView(relativeLayout);
			isFullScreen = false;
			return;
		}
		parent.RemoveView(relativeLayout);
		relativeLayout?.RemoveView(textBlock);
		fullScreenLayout = new(Platform.AppContext)
		{
			LayoutParameters = new CoordinatorLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
			{
				Gravity = (int)GravityFlags.Bottom,
				BottomMargin = 10,
			}
		};
		fullScreenLayout.AddView(textBlock);

		viewGroup.AddView(fullScreenLayout);
		isFullScreen = true;
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
		relativeLayout = new RelativeLayout(Platform.AppContext)
		{
			LayoutParameters = new CoordinatorLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
			{
				Gravity = (int)GravityFlags.Bottom,
				BottomMargin = 10,
			}
		};
		relativeLayout.AddView(textBlock);
		dispatcher.Dispatch(() => parent.AddView(relativeLayout));
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
				textBlock.Text = cue.Text;
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
				parent.RemoveView(relativeLayout);
				relativeLayout?.RemoveView(textBlock);
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