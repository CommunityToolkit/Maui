using CommunityToolkit.Maui.MediaElement.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler : ViewHandler<Views.MediaElement, MauiMediaElement>
{
	/// <inheritdoc/>
	/// <exception cref="NullReferenceException">Thrown if <see cref="MauiContext"/> is <see langword="null"/>.</exception>
	protected override MauiMediaElement CreatePlatformView()
	{
		mediaManager ??= new(MauiContext ?? throw new NullReferenceException(), VirtualView);
		var playerView = mediaManager.CreatePlatformView();
		return new (playerView);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		platformView.Dispose();
		base.DisconnectHandler(platformView);
	}

	/// <inheritdoc/>
	public static void ShouldLoopPlayback(MediaElementHandler handler, Views.MediaElement MediaElement)
	{
		handler.mediaManager?.UpdateShouldLoopPlayback();
	}
}