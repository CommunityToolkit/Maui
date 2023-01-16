using Microsoft.UI.Xaml.Controls;
using Grid = Microsoft.UI.Xaml.Controls.Grid;

namespace CommunityToolkit.Maui.MediaPlayer.PlatformView;

/// <summary>
/// The user-interface element that represents the <see cref="MediaPlayer"/> on Windows.
/// </summary>
public class MauiMediaPlayer : Grid
{
	readonly MediaPlayerElement mediaPlayerElement;

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaPlayer"/> class.
	/// </summary>
	/// <param name="mediaPlayerElement"></param>
	public MauiMediaPlayer(MediaPlayerElement mediaPlayerElement)
	{
		this.mediaPlayerElement = mediaPlayerElement;
		Children.Add(this.mediaPlayerElement);
	}

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MauiMediaPlayer"/>.
	/// </summary>
	public void Dispose()
	{
		if (mediaPlayerElement is not null)
		{
			mediaPlayerElement?.MediaPlayer.Dispose();
		}
	}
}