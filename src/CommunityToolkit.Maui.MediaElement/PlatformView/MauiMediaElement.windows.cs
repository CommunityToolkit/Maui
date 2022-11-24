using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Playback;
using Windows.Storage;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;

public class MauiMediaElement : Grid
{
	private MediaPlayerElement? _mediaPlayerElement;
	public MauiMediaElement(MediaPlayerElement mediaPlayerElement)
	{
		_mediaPlayerElement = mediaPlayerElement;
		Children.Add(_mediaPlayerElement);
	}

	public void Dispose()
	{
		_mediaPlayerElement?.MediaPlayer.Dispose();
		_mediaPlayerElement = null;
	}
}