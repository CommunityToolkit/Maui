using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using Microsoft.UI.Xaml.Media;
using Grid = Microsoft.Maui.Controls.Grid;

namespace CommunityToolkit.Maui.Extensions;

partial class SubtitleExtensions : Grid, IDisposable
{
	bool disposedValue;
	bool isFullScreen = false;

	readonly Microsoft.UI.Xaml.Controls.TextBlock subtitleTextBlock;
	readonly MauiMediaElement? mauiMediaElement;

	public SubtitleExtensions(Microsoft.UI.Xaml.Controls.MediaPlayerElement player)
	{
		mauiMediaElement = player?.Parent as MauiMediaElement;
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

	public void StartSubtitleDisplay()
	{
		Timer = new System.Timers.Timer(1000);
		Dispatcher.Dispatch(() => mauiMediaElement?.Children.Add(subtitleTextBlock));
		Timer.Elapsed += UpdateSubtitle;
		Timer.Start();
	}

	public void StopSubtitleDisplay()
	{
		Cues?.Clear();
		subtitleTextBlock.Text = string.Empty;
		if (Timer is null)
		{
			return;
		}
		Timer.Stop();
		Timer.Elapsed -= UpdateSubtitle;
		if(mauiMediaElement is null)
		{
			return;
		}
		Dispatcher.Dispatch(() => mauiMediaElement?.Children.Remove(subtitleTextBlock));
	}

	void UpdateSubtitle(object? sender, System.Timers.ElapsedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(MediaElement);
		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl) || Cues is null)
		{
			return;
		}
		var cue = Cues.Find(c => c.StartTime <= MediaElement.Position && c.EndTime >= MediaElement.Position);
		Dispatcher.Dispatch(() =>
		{
			if (cue is not null)
			{
				subtitleTextBlock.Text = cue.Text;
				subtitleTextBlock.FontFamily = new FontFamily(new Core.FontExtensions.FontFamily(MediaElement.SubtitleFont).WindowsFont);
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
		ArgumentNullException.ThrowIfNull(mauiMediaElement);
		ArgumentNullException.ThrowIfNull(MediaElement);
		if (e.Grid is not Microsoft.UI.Xaml.Controls.Grid gridItem || string.IsNullOrEmpty(MediaElement.SubtitleUrl))
		{
			return;
		}
		subtitleTextBlock.Text = string.Empty;
		switch (isFullScreen)
		{
			case true:
				subtitleTextBlock.Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 20);
				subtitleTextBlock.FontSize = MediaElement.SubtitleFontSize;
				Dispatcher.Dispatch(() => { gridItem.Children.Remove(subtitleTextBlock); mauiMediaElement.Children.Add(subtitleTextBlock); });
				isFullScreen = false;
				break;
			case false:
				subtitleTextBlock.FontSize = MediaElement.SubtitleFontSize + 8.0;
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
			if (Timer is not null)
			{
				Timer.Stop();
				Timer.Elapsed -= UpdateSubtitle;
			}

			if (disposing)
			{
				Timer?.Dispose();
			}
			Timer = null;
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
