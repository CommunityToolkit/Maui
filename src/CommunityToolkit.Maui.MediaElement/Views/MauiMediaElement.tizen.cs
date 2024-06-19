using CommunityToolkit.Maui.Views;
using Tizen.NUI.BaseComponents;
using Tizen.UIExtensions.NUI;
using MSize = Tizen.Multimedia.Size;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on Tizen.
/// </summary>
public class MauiMediaElement : ViewGroup
{
	VideoView videoView;

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="videoView">The VideoView that acts as the platform media player.</param>
	public MauiMediaElement(VideoView videoView)
	{
		this.videoView = videoView;
		Children.Add(videoView);
	}

	/// <summary>
	/// Gets the dimensions for the currently loaded video.
	/// </summary>
	/// <returns>An object that holds the width and height of the currently loaded video.</returns>
	public async Task<MSize> GetVideoSize()
	{
		if (player.State == PlayerState.Idle)
		{
			if (tcsForStreamInfo == null || tcsForStreamInfo.Task.IsCompleted)
			{
				tcsForStreamInfo = new TaskCompletionSource<bool>();
			}
			await tcsForStreamInfo.Task;
		}
		await TaskPrepare;

		var videoSize = player.StreamInfo.GetVideoProperties().Size;
		return new MSize(videoSize.Width, videoSize.Height);
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