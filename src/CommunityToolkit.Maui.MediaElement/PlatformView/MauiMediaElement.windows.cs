using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Playback;
using Windows.Storage;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;

public class MauiMediaElement : Grid
{
	public MauiMediaElement(MediaPlayerElement mediaPlayerElement)
	{
		Children.Add(mediaPlayerElement);
	}
}