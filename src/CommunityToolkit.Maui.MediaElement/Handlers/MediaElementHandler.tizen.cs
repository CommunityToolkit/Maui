using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>
{
	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
	public static void ShouldLoopPlayback(MediaElementHandler handler, MediaElement MediaElement)
	{
		ArgumentNullException.ThrowIfNull(handler);

		handler.MediaManager?.UpdateShouldLoopPlayback();
	}

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
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="platformView"/> is <see langword="null"/>.</exception>
	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		ArgumentNullException.ThrowIfNull(platformView);

		platformView.Dispose();
		base.DisconnectHandler(platformView);
	}
}