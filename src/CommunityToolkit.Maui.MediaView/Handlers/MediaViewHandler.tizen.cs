using CommunityToolkit.Maui.MediaView.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaView;

public partial class MediaViewHandler : ViewHandler<MediaView, MauiMediaView>
{
	/// <inheritdoc/>
	/// <exception cref="NullReferenceException">Thrown if <see cref="MauiContext"/> is <see langword="null"/>.</exception>
	protected override MauiMediaView CreatePlatformView()
	{
		mediaManager ??= new(MauiContext ?? throw new NullReferenceException(), VirtualView);
		var playerView = mediaManager.CreatePlatformView();
		return new (playerView);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaView platformView)
	{
		platformView.Dispose();
		base.DisconnectHandler(platformView);
	}

	/// <inheritdoc/>
	public static void ShouldLoopPlayback(MediaViewHandler handler, MediaView MediaView)
	{
		handler.mediaManager?.UpdateShouldLoopPlayback();
	}
}