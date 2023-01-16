using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.MediaPlayer.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaPlayer;

public partial class MediaPlayerHandler : ViewHandler<Views.MediaPlayer, MauiMediaPlayer>, IDisposable
{
	/// <summary>
	/// Maps the <see cref="IMediaPlayer.ShouldLoopPlayback"/> property between the abstract
	/// <see cref="MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="Views.MediaPlayer"/> instance.</param>
	public static void ShouldLoopPlayback(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer)
	{
		handler.mediaManager?.UpdateShouldLoopPlayback();
	}

	protected override MauiMediaPlayer CreatePlatformView()
	{
		mediaManager ??= new(MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} cannot be null"), VirtualView);
		var (_, playerView) = mediaManager.CreatePlatformView();
		return new(Context, playerView);
	}

	protected override void DisconnectHandler(MauiMediaPlayer platformView)
	{
		platformView.Dispose();
		Dispose();
		base.DisconnectHandler(platformView);
	}
}