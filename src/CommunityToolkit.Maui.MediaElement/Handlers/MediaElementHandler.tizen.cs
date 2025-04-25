using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>
{
	/// <inheritdoc/>
	/// <exception cref="NullReferenceException">Thrown if <see cref="MauiContext"/> is <see langword="null"/>.</exception>
	protected override MauiMediaElement CreatePlatformView()
	{
		MediaManager ??= new(MauiContext ?? throw new NullReferenceException(), 
								VirtualView,
								Dispatcher.GetForCurrentThread() ?? throw new InvalidOperationException($"{nameof(IDispatcher)} cannot be null"));

		var playerView = MediaManager.CreatePlatformView();
		return new (playerView);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		platformView.Dispose();
		base.DisconnectHandler(platformView);
	}

	/// <inheritdoc/>
	public static void ShouldLoopPlayback(MediaElementHandler handler, MediaElement MediaElement)
	{
		handler.MediaManager?.UpdateShouldLoopPlayback();
	}
}