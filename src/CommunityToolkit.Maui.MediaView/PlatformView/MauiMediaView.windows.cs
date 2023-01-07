using Microsoft.UI.Xaml.Controls;
using Grid = Microsoft.UI.Xaml.Controls.Grid;

namespace CommunityToolkit.Maui.MediaView.PlatformView;

/// <summary>
/// The user-interface element that represents the <see cref="MediaView"/> on Windows.
/// </summary>
public class MauiMediaView : Grid
{
	readonly MediaPlayerElement mediaPlayerElement;

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaView"/> class.
	/// </summary>
	/// <param name="mediaPlayerElement"></param>
	public MauiMediaView(MediaPlayerElement mediaPlayerElement)
	{
		this.mediaPlayerElement = mediaPlayerElement;
		Children.Add(this.mediaPlayerElement);
	}

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MauiMediaView"/>.
	/// </summary>
	public void Dispose()
	{
		if (mediaPlayerElement is not null)
		{
			mediaPlayerElement?.MediaPlayer.Dispose();
		}
	}
}