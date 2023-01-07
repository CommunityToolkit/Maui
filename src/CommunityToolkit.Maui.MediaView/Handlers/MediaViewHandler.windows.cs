using CommunityToolkit.Maui.MediaView.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaView;

public partial class MediaViewHandler : ViewHandler<MediaView, MauiMediaView>, IDisposable
{
	/// <summary>
	/// Maps the <see cref="IMediaView.ShouldLoopPlayback"/> property between the abstract
	/// <see cref="MediaView"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaView">The associated <see cref="MediaView"/> instance.</param>
	public static void ShouldLoopPlayback(MediaViewHandler handler, MediaView MediaView)
	{
		handler.mediaManager?.UpdateShouldLoopPlayback();
	}

	/// <inheritdoc/>
	protected override MauiMediaView CreatePlatformView()
	{
		mediaManager ??= new(MauiContext ?? throw new NullReferenceException(), VirtualView);
		var mediaPlatform = mediaManager.CreatePlatformView();
		return new(mediaPlatform);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaView platformView)
	{
		platformView.Dispose();
		Dispose();
		base.DisconnectHandler(platformView);
	}
}