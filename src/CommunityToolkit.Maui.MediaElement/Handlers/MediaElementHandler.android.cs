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

		var playerView = MediaManager.CreatePlatformView(VirtualView.AndroidViewType, VirtualView.IsAndroidForegroundServiceEnabled);
		return new(Context, playerView);
	}
	protected override async void ConnectHandler(MauiMediaElement platformView)
	{
		base.ConnectHandler(platformView);
		if (platformView is null)
		{
			throw new InvalidOperationException($"{nameof(platformView)} cannot be null");
		}
		if (MediaManager is null)
		{
			throw new InvalidOperationException($"{nameof(MediaManager)} cannot be null");
		}

		try
		{
			var player = await MediaManager.CreatePlatformPlayer();
			platformView.SetView(player);
			await MediaManager.SynchronizePlayerStateAsync();
		}
		catch (Exception exception)
		{
			((IMediaElement)VirtualView).MediaFailed(new MediaFailedEventArgs(exception.Message));
		}
	}
	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		Dispose();
		platformView.Dispose();
		base.DisconnectHandler(platformView);
	}
}