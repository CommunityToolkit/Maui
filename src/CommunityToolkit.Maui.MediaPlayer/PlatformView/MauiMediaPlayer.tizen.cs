using Tizen.NUI.BaseComponents;
using Tizen.UIExtensions.NUI;

namespace CommunityToolkit.Maui.MediaPlayer.PlatformView;

/// <summary>
/// The user-interface element that represents the <see cref="MediaPlayer"/> on Tizen.
/// </summary>
public class MauiMediaPlayer : ViewGroup
{
	VideoView videoView;

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaPlayer"/> class.
	/// </summary>
	/// <param name="videoView">The VideoView that acts as the platform media player.</param>
	public MauiMediaPlayer(VideoView videoView)
	{
		this.videoView = videoView;
		Children.Add(videoView);
	}
	
	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="MediaPlayer"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (videoView is not null)
			{
				videoView.Dispose();
			}
		}

		base.Dispose(disposing);
	}
}