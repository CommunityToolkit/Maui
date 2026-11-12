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
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
	public static void ShouldLoopPlayback(MediaElementHandler handler, MediaElement mediaElement)
	{
		ArgumentNullException.ThrowIfNull(handler);

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

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="platformView"/> is <see langword="null"/>.</exception>
	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		ArgumentNullException.ThrowIfNull(platformView);

		platformView.FullScreenStateChanged -= OnScreenStateChanged;

		platformView.Dispose();
		Dispose();
		base.DisconnectHandler(platformView);
	}
}