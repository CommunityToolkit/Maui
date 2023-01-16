using CommunityToolkit.Maui.MediaPlayer.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaPlayer;

public partial class MediaPlayerHandler : ViewHandler<MediaPlayer, MauiMediaPlayer>, IDisposable
{
	/// <summary>
	/// Maps the <see cref="IMediaPlayer.ShouldLoopPlayback"/> property between the abstract
	/// <see cref="MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="MediaPlayer"/> instance.</param>
	public static void ShouldLoopPlayback(MediaPlayerHandler handler, MediaPlayer MediaPlayer)
	{
		handler.mediaManager?.UpdateShouldLoopPlayback();
	}

	/// <inheritdoc/>
	protected override MauiMediaPlayer CreatePlatformView()
	{
		mediaManager ??= new(MauiContext ?? throw new NullReferenceException(), VirtualView);
		var mediaPlatform = mediaManager.CreatePlatformView();
		return new(mediaPlatform);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaPlayer platformView)
	{
		platformView.Dispose();
		Dispose();
		base.DisconnectHandler(platformView);
	}
}