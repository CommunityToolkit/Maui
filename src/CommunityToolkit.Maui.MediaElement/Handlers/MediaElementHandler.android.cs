using CommunityToolkit.Maui.MediaElement.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>, IDisposable
{
	/// <summary>
	/// Maps the <see cref="IMediaElement.ShouldLoopPlayback"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapIsLooping(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.mediaManager?.UpdateIsLooping();
	}

	protected override MauiMediaElement CreatePlatformView()
	{
		mediaManager ??= new(MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} cannot be null"), VirtualView);
		var (_, playerView) = mediaManager.CreatePlatformView();
		return new(Context, playerView);
	}

	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		platformView.Dispose();
		Dispose();
		base.DisconnectHandler(platformView);
	}
}