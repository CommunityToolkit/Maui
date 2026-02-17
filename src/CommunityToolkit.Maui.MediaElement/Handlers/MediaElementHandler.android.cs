using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>, IDisposable
{
	/// <summary>
	/// Maps the <see cref="IMediaElement.ShouldLoopPlayback"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void ShouldLoopPlayback(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.MediaManager?.UpdateShouldLoopPlayback();
	}


	protected override MauiMediaElement CreatePlatformView()
	{
		MediaManager ??= new(MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} cannot be null"),
								VirtualView,
								Dispatcher.GetForCurrentThread() ?? throw new InvalidOperationException($"{nameof(IDispatcher)} cannot be null"));

		var (_, playerView) = MediaManager.CreatePlatformView(VirtualView.AndroidViewType, VirtualView.IsAndroidForegroundServiceEnabled);
		return new(Context, playerView);
	}

	protected override void ConnectHandler(MauiMediaElement platformView)
	{
		platformView.FullScreenStateChanged += OnScreenStateChanged;
		base.ConnectHandler(platformView);
	}

	void OnScreenStateChanged(object? sender, ScreenStateChangedEventArgs e)
	{
		MediaManager?.UpdateFullScreenState(e.NewState);
	}
	/// <summary>
	/// Disconnects the handler from the platform-specific <see cref="MauiMediaElement"/> and
	/// unsubscribes from related events, disposing the platform view and handler resources.
	/// </summary>
	/// <param name="platformView">The platform-specific <see cref="MauiMediaElement"/> instance to disconnect.</param>
	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		platformView.FullScreenStateChanged -= OnScreenStateChanged;
		platformView.Dispose();
		Dispose();
		base.DisconnectHandler(platformView);
	}
}