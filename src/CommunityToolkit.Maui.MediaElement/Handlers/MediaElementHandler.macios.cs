using AVKit;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>, IDisposable
{
	AVPlayerViewController? playerViewController;

	/// <inheritdoc/>
	/// <exception cref="NullReferenceException">Thrown if <see cref="MauiContext"/> is <see langword="null"/>.</exception>
	protected override MauiMediaElement CreatePlatformView()
	{
		if (MauiContext is null)
		{
			throw new InvalidOperationException($"{nameof(MauiContext)} cannot be null");
		}

		mediaManager ??= new(MauiContext,
			VirtualView,
			Dispatcher.GetForCurrentThread() ?? throw new InvalidOperationException($"{nameof(IDispatcher)} cannot be null"));

		// get the options from the MediaElement constructor
		MediaElementOptions options = VirtualView.MediaElementOptions;

		(_, playerViewController) = mediaManager.CreatePlatformView(options);

		return new(playerViewController, VirtualView);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		platformView.Dispose();
		Dispose();

		base.DisconnectHandler(platformView);
	}

	partial void PlatformDispose()
	{
		playerViewController?.Dispose();
		playerViewController = null;
	}
}