using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using Grid = Microsoft.Maui.Controls.Grid;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Span = Microsoft.UI.Xaml.Documents.Span;
using TextAlignment = Microsoft.UI.Xaml.TextAlignment;
using Thickness = Microsoft.UI.Xaml.Thickness;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
using Visibility = Microsoft.UI.Xaml.Visibility;

namespace CommunityToolkit.Maui.Extensions;

partial class SubtitleExtensions : Grid, IDisposable
{
	bool disposedValue;
	bool isFullScreen = false;
	readonly TextBlock subtitleTextBlock;
	readonly MauiMediaElement? mauiMediaElement;
	public List<SubtitleCue>? Cues { get; set; }

	public SubtitleExtensions(MediaPlayerElement player)
	{
		mauiMediaElement = player?.Parent as MauiMediaElement;
		MediaManager.FullScreenEvents.WindowsChanged += OnFullScreenChanged;

		subtitleTextBlock = new TextBlock
		{
			Text = string.Empty,
			Margin = new Thickness(0, 0, 0, 20),
			Visibility = Visibility.Collapsed,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom,
			Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
			TextWrapping = TextWrapping.Wrap,
		};
	}

	public void StartSubtitleDisplay()
	{
		Timer = new System.Timers.Timer(1000);
		Dispatcher.Dispatch(() => mauiMediaElement?.Children.Add(subtitleTextBlock));
		Timer.Elapsed += UpdateSubtitle;
		Timer.Start();
		Cues = Document?.Cues;
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
		if (mauiMediaElement is null)
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
				DisplayCue(cue);
			}
			else
			{
				subtitleTextBlock.Text = string.Empty;
				subtitleTextBlock.Visibility = Visibility.Collapsed;
			}
		});
	}

	void DisplayCue(SubtitleCue cue)
	{
		if(cue.ParsedCueText is null)
		{
			return;
		}
		subtitleTextBlock.Inlines.Clear();
		ProcessCueText(subtitleTextBlock.Inlines, cue.ParsedCueText);
		ApplyStyles(cue);
		subtitleTextBlock.Visibility = Visibility.Visible;

		subtitleTextBlock.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
		subtitleTextBlock.LineHeight = subtitleTextBlock.FontSize * 1.2;
	}

	static void ProcessCueText(InlineCollection inlines, SubtitleNode node)
	{
		foreach (var child in node.Children)
		{
			if (child.NodeType == "text")
			{
				string? text = child.TextContent;
				if (!string.IsNullOrEmpty(text))
				{ 				
					inlines.Add(new Run { Text = text });
				}
			}
			else if(child.NodeType is not null)
			{
				var span = new Span();
				ApplyStyleToSpan(span, child.NodeType);
				ProcessCueText(span.Inlines, child);
				inlines.Add(span);
			}
		}
	}

	static void ApplyStyleToSpan(Span span, string nodeType)
	{
		switch (nodeType.ToLower())
		{
			case "b":
				span.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
				break;
			case "i":
				span.FontStyle = FontStyle.Italic;
				break;
			case "u":
				span.TextDecorations = Windows.UI.Text.TextDecorations.Underline;
				break;
			case "v":
				span.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Yellow);
				break;
		}
	}

	void ApplyStyles(SubtitleCue cue)
	{
		if(MediaElement?.SubtitleUrl is null || mauiMediaElement?.Width is null)
		{
			return;
		}
		subtitleTextBlock.TextAlignment = GetTextAlignment(cue.Align);
		subtitleTextBlock.FontFamily = new FontFamily(new Core.FontExtensions.FontFamily(MediaElement.SubtitleFont).WindowsFont);

		if (!string.IsNullOrEmpty(cue.Position))
		{
			var parts = cue.Position.Split(',');
			if (parts.Length > 0 && float.TryParse(parts[0].TrimEnd('%'), out float horizontalPosition))
			{
				subtitleTextBlock.Margin = new Thickness(horizontalPosition * mauiMediaElement.Width / 100, 0, 0, subtitleTextBlock.Margin.Bottom);
			}
		}

		if (!string.IsNullOrEmpty(cue.Line) && float.TryParse(cue.Line.TrimEnd('%'), out float verticalPosition))
		{
			subtitleTextBlock.Margin = new Thickness(subtitleTextBlock.Margin.Left, 0, 0, verticalPosition * mauiMediaElement.Height / 100);
		}
		if (cue.Vertical is null)
		{
			return;
		}
		ApplyVerticalWriting(cue.Vertical);
	}

	static TextAlignment GetTextAlignment(string align)
	{
		return align?.ToLower() switch
		{
			"left" => TextAlignment.Left,
			"right" => TextAlignment.Right,
			"center" => TextAlignment.Center,
			_ => TextAlignment.Center,
		};
	}

	void ApplyVerticalWriting(string vertical)
	{
		if (vertical == "rl" || vertical == "lr")
		{
			subtitleTextBlock.RenderTransform = new RotateTransform
			{
				Angle = vertical == "rl" ? 90 : -90
			};
			subtitleTextBlock.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
		}
		else
		{
			subtitleTextBlock.RenderTransform = null;
		}
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
		subtitleTextBlock.Text = string.Empty;
		switch (isFullScreen)
		{
			case true:
				subtitleTextBlock.Margin = new Thickness(0, 0, 0, 20);
				subtitleTextBlock.FontSize = MediaElement.SubtitleFontSize;
				Dispatcher.Dispatch(() => { gridItem.Children.Remove(subtitleTextBlock); mauiMediaElement.Children.Add(subtitleTextBlock); });
				isFullScreen = false;
				break;
			case false:
				subtitleTextBlock.FontSize = MediaElement.SubtitleFontSize + 8.0;
				subtitleTextBlock.Margin = new Thickness(0, 0, 0, 300);
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
