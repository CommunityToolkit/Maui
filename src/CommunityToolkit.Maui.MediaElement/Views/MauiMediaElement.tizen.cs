using CommunityToolkit.Maui.Views;
using Tizen.NUI.BaseComponents;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on Tizen.
/// </summary>
public class MauiMediaElement : Tizen.NUI.BaseComponents.View
{
	VideoView videoView;

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="videoView">The VideoView that acts as the platform media player.</param>
	public MauiMediaElement(VideoView videoView)
	{
		this.videoView = videoView;
		Add(videoView);
	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="MediaElement"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			videoView?.Dispose();
		}

		base.Dispose(disposing);
	}
}