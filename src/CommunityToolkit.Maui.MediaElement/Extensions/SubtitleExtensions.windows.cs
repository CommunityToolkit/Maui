using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// A class that provides subtitle support for a video player.
/// </summary>
public partial class SubtitleExtensions : Grid, IDisposable
{
	bool isFullScreen = false;
	readonly HttpClient httpClient;
	Microsoft.UI.Xaml.Controls.Grid? item;
	System.Timers.Timer? timer;
	List<SubtitleCue> cues;
	bool disposedValue;
	IMediaElement? mediaElement;
	Microsoft.UI.Xaml.Controls.TextBlock? xamlTextBlock;
	MauiMediaElement? mauiMediaElement;

	/// <summary>
	/// The SubtitleExtensions class provides a way to display subtitles on a video player.
	/// </summary>
	public SubtitleExtensions()
	{
		httpClient = new();
		cues = [];
		MauiMediaElement.WindowsChanged += MauiMediaElement_WindowsChanged;
	}
	void MauiMediaElement_WindowsChanged(object? sender, WindowsEventArgs e)
	{
		if (mauiMediaElement is null || e.data is not Microsoft.UI.Xaml.Controls.Grid gridItem || string.IsNullOrEmpty(mediaElement?.SubtitleUrl))
		{
			return;
		}
		this.item = gridItem;
		switch(isFullScreen)
		{
			case true:
				Dispatcher.Dispatch(() => { item.Children.Remove(xamlTextBlock); mauiMediaElement.Children.Add(xamlTextBlock); });
				isFullScreen = false;
				break;
			case false:
				Dispatcher.Dispatch(() => { mauiMediaElement.Children.Remove(xamlTextBlock); item.Children.Add(xamlTextBlock); });
				isFullScreen = true;
				break;
		}
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
		xamlTextBlock ??= new()
		{
			Text = string.Empty,
			Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 10),
			Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
			HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
			VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Bottom,
			Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
			FontSize = 16,
			TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
		};
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
		Dispatcher.Dispatch(() => mauiMediaElement?.Children.Add(xamlTextBlock));
		timer = new System.Timers.Timer(1000);
		timer.Elapsed += Timer_Elapsed;
		timer.Start();
	}

	void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
	{
		if (mediaElement?.Position is null || cues.Count == 0 || string.IsNullOrEmpty(mediaElement.SubtitleUrl) || xamlTextBlock is null)
		{
			return;
		}
		var cue = cues.Find(c => c.StartTime <= mediaElement.Position && c.EndTime >= mediaElement.Position);
		Dispatcher.Dispatch(() =>
		{
			if (cue is not null)
			{
				xamlTextBlock.Text = cue.Text;
				xamlTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
			}
			else
			{
				xamlTextBlock.Text = string.Empty;
				xamlTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
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
		timer.Stop();
		timer.Elapsed -= Timer_Elapsed;
		if(mauiMediaElement is null)
		{
			return;
		}
		Dispatcher.Dispatch(() => mauiMediaElement.Children.Remove(xamlTextBlock));
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
				timer.Elapsed -= Timer_Elapsed;
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
