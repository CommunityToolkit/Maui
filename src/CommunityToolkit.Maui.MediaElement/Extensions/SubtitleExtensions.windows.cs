using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using Microsoft.UI.Xaml.Media;

namespace CommunityToolkit.Maui.Extensions;

class SubtitleExtensions : Grid, IDisposable
{
	bool disposedValue;
	bool isFullScreen = false;

	static readonly HttpClient httpClient = new();
	readonly Microsoft.UI.Xaml.Controls.TextBlock subtitleTextBlock;

	List<SubtitleCue> cues;
	IMediaElement? mediaElement;
	MauiMediaElement? mauiMediaElement;
	System.Timers.Timer? timer;

	/// <summary>
	/// The SubtitleExtensions class provides a way to display subtitles on a video player.
	/// </summary>
	public SubtitleExtensions()
	{
		cues = [];
		MauiMediaElement.GridEventsChanged += OnFullScreenChanged;
		subtitleTextBlock = new()
		{
			Text = string.Empty,
			Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 20),
			Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
			HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
			VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Bottom,
			Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
			TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
		};
	}

	/// <summary>
	/// Loads the subtitles from the provided URL.
	/// </summary>
	/// <param name="mediaElement"></param>
	/// <param name="player"></param>
	public async Task LoadSubtitles(IMediaElement mediaElement, Microsoft.UI.Xaml.Controls.MediaPlayerElement player)
	{
		this.mediaElement = mediaElement;
		mauiMediaElement = player?.Parent as MauiMediaElement;
		cues.Clear();
		subtitleTextBlock.Text = string.Empty;
		subtitleTextBlock.FontSize = mediaElement.SubtitleFontSize;
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
		timer = new System.Timers.Timer(1000);
		Dispatcher.Dispatch(() => mauiMediaElement?.Children.Add(subtitleTextBlock));
		timer.Elapsed += UpdateSubtitle;
		timer.Start();
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
		timer.Stop();
		timer.Elapsed -= UpdateSubtitle;
		if(mauiMediaElement is null)
		{
			return;
		}
		Dispatcher.Dispatch(() => mauiMediaElement?.Children.Remove(subtitleTextBlock));
	}

	void UpdateSubtitle(object? sender, System.Timers.ElapsedEventArgs e)
	{
		if (string.IsNullOrEmpty(mediaElement?.SubtitleUrl))
		{
			return;
		}
		var cue = cues.Find(c => c.StartTime <= mediaElement.Position && c.EndTime >= mediaElement.Position);
		Dispatcher.Dispatch(() =>
		{
			if (cue is not null)
			{
				subtitleTextBlock.Text = cue.Text;
				subtitleTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
			}
			else
			{
				subtitleTextBlock.Text = string.Empty;
				subtitleTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
			}
		});
	}

	void OnFullScreenChanged(object? sender, GridEventArgs e)
	{
		if (e.Grid is not Microsoft.UI.Xaml.Controls.Grid gridItem || string.IsNullOrEmpty(mediaElement?.SubtitleUrl))
		{
			return;
		}
		ArgumentNullException.ThrowIfNull(mauiMediaElement);
		subtitleTextBlock.Text = string.Empty;
		switch (isFullScreen)
		{
			case true:
				subtitleTextBlock.FontSize = mediaElement.SubtitleFontSize;
				subtitleTextBlock.FontFamily = new FontFamily(mediaElement.SubtitleFont);
				subtitleTextBlock.Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 20);
				Dispatcher.Dispatch(() => { gridItem.Children.Remove(subtitleTextBlock); mauiMediaElement.Children.Add(subtitleTextBlock); });
				isFullScreen = false;
				break;
			case false:
				subtitleTextBlock.FontSize = mediaElement.SubtitleFontSize + 8.0;
				subtitleTextBlock.Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 300);
				Dispatcher.Dispatch(() => { mauiMediaElement.Children.Remove(subtitleTextBlock); gridItem.Children.Add(subtitleTextBlock); });
				isFullScreen = true;
				break;
		}
	}

	/// <summary>
	/// The Dispose method. For the <see cref="SubtitleExtensions"/> class."/>
	/// </summary>
	/// <param name="disposing"></param>
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
				httpClient?.Dispose();
				timer?.Dispose();
			}
			timer = null;
			disposedValue = true;
		}
	}

	/// <summary>
	/// A finalizer for the <see cref="SubtitleExtensions"/>.
	/// </summary>
	~SubtitleExtensions()
	{
	     Dispose(disposing: false);
	}

	/// <summary>
	/// 
	/// </summary>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
