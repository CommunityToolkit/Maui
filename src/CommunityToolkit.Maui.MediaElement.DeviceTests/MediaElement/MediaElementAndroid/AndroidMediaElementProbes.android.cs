using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

public sealed class AndroidMediaElementProbeHandler : MediaElementHandler
{
	public MediaManager? Manager => MediaManager;

	// Reaches CreatePlatformView again, or for the first time on a handler that has no MauiContext.
	public MauiMediaElement CreatePlatformViewDirectly() => CreatePlatformView();

	// Reaches the toolkit's DisconnectHandler directly, without MAUI's guard around it.
	public void DisconnectPlatformView(MauiMediaElement platformView) => DisconnectHandler(platformView);

	// How a test reaches the finalizer path (disposing: false).
	public void InvokeDispose(bool disposing) => Dispose(disposing);
}

public sealed class AndroidMediaManagerProbe(IMauiContext context, IMediaElement mediaElement, IDispatcher dispatcher) : MediaManager(context, mediaElement, dispatcher)
{
	public PlayerView? ExposedPlayerView => PlayerView;

	public IExoPlayer? ExposedPlayer => Player;

	// A subclass can set Player without CreatePlatformView ever running: the manager then has a player but no PlayerView.
	public void AttachPlayer(IExoPlayer player) => Player = player;

	public void InvokeDispose(bool disposing) => Dispose(disposing);
}