using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using Microsoft.UI.Xaml.Media;
using Grid = Microsoft.Maui.Controls.Grid;

namespace CommunityToolkit.Maui.Extensions;

partial class SubtitleExtensions : Grid, IDisposable
{
	bool disposedValue;
	bool isFullScreen = false;
	readonly Microsoft.UI.Xaml.Controls.TextBox subtitleTextBlock;
	readonly MauiMediaElement? mauiMediaElement;
	readonly int width;

	public SubtitleExtensions(Microsoft.UI.Xaml.Controls.MediaPlayerElement player)
	{
		width = (int)player.ActualWidth / 3;
		mauiMediaElement = player.Parent as MauiMediaElement;
		MediaManager.FullScreenEvents.WindowsChanged += OnFullScreenChanged;
		subtitleTextBlock = new()
		{
			FontSize = 16,
			Width = width,
			TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
			Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 20),
			Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
			HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
			VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Bottom,
			Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
			TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
		};
		subtitleTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
		subtitleTextBlock.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
		subtitleTextBlock.BackgroundSizing = Microsoft.UI.Xaml.Controls.BackgroundSizing.InnerBorderEdge;
		subtitleTextBlock.Opacity = 0.7;
		subtitleTextBlock.TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap;
		subtitleTextBlock.Text = string.Empty;
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
		subtitleTextBlock.ClearValue(Microsoft.UI.Xaml.Controls.TextBox.TextProperty);
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
				subtitleTextBlock.HorizontalTextAlignment = Microsoft.UI.Xaml.TextAlignment.Center;
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

	void OnFullScreenChanged(object? sender, FullScreenStateChangedEventArgs e)
	{
		var gridItem = MediaManager.FullScreenEvents.grid;
		ArgumentNullException.ThrowIfNull(mauiMediaElement);
		ArgumentNullException.ThrowIfNull(MediaElement);
		ArgumentNullException.ThrowIfNull(gridItem);
		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl))
		{
			return;
		}

		switch (isFullScreen)
		{
			case true:
				subtitleTextBlock.Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 20);
				subtitleTextBlock.FontSize = MediaElement.SubtitleFontSize;
				subtitleTextBlock.Width = width;
				Dispatcher.Dispatch(() => { gridItem.Children.Remove(subtitleTextBlock); mauiMediaElement.Children.Add(subtitleTextBlock); });
				isFullScreen = false;
				break;
			case false:
				subtitleTextBlock.FontSize = MediaElement.SubtitleFontSize + 8.0;
				subtitleTextBlock.Width = DeviceDisplay.Current.MainDisplayInfo.Width / 4;
				subtitleTextBlock.Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 100);
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
