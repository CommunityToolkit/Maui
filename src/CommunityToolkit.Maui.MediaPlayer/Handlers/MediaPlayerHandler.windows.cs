using CommunityToolkit.Maui.MediaPlayer.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaPlayer;

public partial class MediaPlayerHandler : ViewHandler<Views.MediaPlayer, MauiMediaPlayer>, IDisposable
{
	/// <summary>
	/// Maps the <see cref="Core.IMediaPlayer.ShouldLoopPlayback"/> property between the abstract
	/// <see cref="MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="Views.MediaPlayer"/> instance.</param>
	public static void ShouldLoopPlayback(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer)
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