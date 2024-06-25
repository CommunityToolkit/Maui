using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using Microsoft.UI.Xaml.Media;

namespace CommunityToolkit.Maui.Extensions;

class SubtitleExtensions : Grid, IDisposable
{
	bool disposedValue;
	bool isFullScreen = false;

	readonly Microsoft.UI.Xaml.Controls.TextBlock subtitleTextBlock;

	List<SubtitleCue> cues;
	IMediaElement? mediaElement;
	MauiMediaElement? mauiMediaElement;
	System.Timers.Timer? timer;

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

	public async Task LoadSubtitles(IMediaElement mediaElement, Microsoft.UI.Xaml.Controls.MediaPlayerElement player)
	{
		this.mediaElement = mediaElement;
		mauiMediaElement = player?.Parent as MauiMediaElement;
		cues.Clear();
		subtitleTextBlock.Text = string.Empty;
		subtitleTextBlock.FontSize = mediaElement.SubtitleFontSize;
		subtitleTextBlock.FontFamily = new FontFamily(mediaElement.SubtitleFont);
		subtitleTextBlock.FontStyle = Windows.UI.Text.FontStyle.Normal;
		SubtitleParser parser;
		
		var content = await SubtitleParser.Content(mediaElement.SubtitleUrl);
		if(mediaElement.CustomSubtitleParser is not null)
		{
			parser = new(mediaElement.CustomSubtitleParser);
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

	public void StartSubtitleDisplay()
	{
		timer = new System.Timers.Timer(1000);
		Dispatcher.Dispatch(() => mauiMediaElement?.Children.Add(subtitleTextBlock));
		timer.Elapsed += UpdateSubtitle;
		timer.Start();
	}

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
				subtitleTextBlock.FontFamily = new FontFamily(new Core.FontExtensions.FontFamily(mediaElement.SubtitleFont).WindowsFont);
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
				subtitleTextBlock.Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 20);
				subtitleTextBlock.FontSize = mediaElement.SubtitleFontSize;
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
				timer?.Dispose();
			}
			timer = null;
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
