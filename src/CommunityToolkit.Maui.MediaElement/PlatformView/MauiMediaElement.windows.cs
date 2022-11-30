using Microsoft.UI.Xaml.Controls;
using Grid = Microsoft.UI.Xaml.Controls.Grid;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;

public class MauiMediaElement : Grid
{
	MediaPlayerElement? mediaPlayerElement;

	public MauiMediaElement(MediaPlayerElement mediaPlayerElement)
	{
		this.mediaPlayerElement = mediaPlayerElement;
		Children.Add(this.mediaPlayerElement);
	}

	public void Dispose()
	{
		if (mediaPlayerElement is not null)
		{
			mediaPlayerElement?.MediaPlayer.Dispose();
			mediaPlayerElement = null;
		}
	}
}