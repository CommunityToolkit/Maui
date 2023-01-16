using CommunityToolkit.Maui.MediaPlayer.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaPlayer;

public partial class MediaPlayerHandler : ViewHandler<Views.MediaPlayer, MauiMediaPlayer>
{
	/// <inheritdoc/>
	/// <exception cref="NullReferenceException">Thrown if <see cref="MauiContext"/> is <see langword="null"/>.</exception>
	protected override MauiMediaPlayer CreatePlatformView()
	{
		mediaManager ??= new(MauiContext ?? throw new NullReferenceException(), VirtualView);
		var playerView = mediaManager.CreatePlatformView();
		return new (playerView);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaPlayer platformView)
	{
		platformView.Dispose();
		base.DisconnectHandler(platformView);
	}

	/// <inheritdoc/>
	public static void ShouldLoopPlayback(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer)
	{
		handler.mediaManager?.UpdateShouldLoopPlayback();
	}
}