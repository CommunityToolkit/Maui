using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Grid = Microsoft.Maui.Controls.Grid;

namespace CommunityToolkit.Maui.Extensions;

partial class SubtitleExtensions : SubtitleTimer<TextBox>
{
	readonly MauiMediaElement? mauiMediaElement;
	readonly int width;

	public SubtitleExtensions(Microsoft.UI.Xaml.Controls.MediaPlayerElement player, IDispatcher dispatcher)
	{	
		this.dispatcher = dispatcher;
		width = (int)player.ActualWidth / 3;
		mauiMediaElement = player.Parent as MauiMediaElement;
		MediaManager.FullScreenEvents.WindowsChanged += OnFullScreenChanged;
		InitializeTextBlock();
	}

	public void StartSubtitleDisplay()
	{
		dispatcher.Dispatch(() => mauiMediaElement?.Children.Add(subtitleTextBlock));
		StartTimer();
	}

	public void StopSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(subtitleTextBlock);
		Cues?.Clear();
		subtitleTextBlock.ClearValue(Microsoft.UI.Xaml.Controls.TextBox.TextProperty);
		StopTimer();
		if(mauiMediaElement is null)
		{
			return;
		}
		dispatcher.Dispatch(() => mauiMediaElement?.Children.Remove(subtitleTextBlock));
	}

	public override void UpdateSubtitle(object? sender, System.Timers.ElapsedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(MediaElement);
		ArgumentNullException.ThrowIfNull(subtitleTextBlock);
		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl) || Cues is null)
		{
			return;
		}
		var cue = Cues.Find(c => c.StartTime <= MediaElement.Position && c.EndTime >= MediaElement.Position);
		dispatcher.Dispatch(() =>
		{
			if (cue is not null)
			{
				InitializeText();
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

	void OnFullScreenChanged(object? sender, FullScreenStateChangedEventArgs e)
	{
		var gridItem = MediaManager.FullScreenEvents.grid;
		ArgumentNullException.ThrowIfNull(mauiMediaElement);
		ArgumentNullException.ThrowIfNull(MediaElement);
		ArgumentNullException.ThrowIfNull(gridItem);
		ArgumentNullException.ThrowIfNull(subtitleTextBlock);
		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl))
		{
			return;
		}

		switch (e.NewState)
		{
			case MediaElementScreenState.Default:
				subtitleTextBlock.Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 20);
				subtitleTextBlock.FontSize = MediaElement.SubtitleFontSize;
				subtitleTextBlock.Width = width;
				dispatcher.Dispatch(() => { gridItem.Children.Remove(subtitleTextBlock); mauiMediaElement.Children.Add(subtitleTextBlock); });
				break;
			case MediaElementScreenState.FullScreen:
				subtitleTextBlock.FontSize = MediaElement.SubtitleFontSize + 8.0;
				subtitleTextBlock.Width = DeviceDisplay.Current.MainDisplayInfo.Width / 4;
				subtitleTextBlock.Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 100);
				dispatcher.Dispatch(() => { mauiMediaElement.Children.Remove(subtitleTextBlock); gridItem.Children.Add(subtitleTextBlock); });
				break;
		}
	}

	void InitializeTextBlock()
	{
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
			Text = string.Empty
		};
	}

	void InitializeText()
	{
		ArgumentNullException.ThrowIfNull(MediaElement);
		ArgumentNullException.ThrowIfNull(subtitleTextBlock);
		subtitleTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
		subtitleTextBlock.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
		subtitleTextBlock.BackgroundSizing = Microsoft.UI.Xaml.Controls.BackgroundSizing.InnerBorderEdge;
		subtitleTextBlock.Opacity = 0.7;
		subtitleTextBlock.TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap;
		subtitleTextBlock.HorizontalTextAlignment = Microsoft.UI.Xaml.TextAlignment.Center;
		subtitleTextBlock.FontFamily = new FontFamily(new Core.FontExtensions.FontFamily(MediaElement.SubtitleFont).WindowsFont);
	}
}
