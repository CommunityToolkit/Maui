using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// The toolkit's handler, plus access to its protected members. Tests register it for <see cref="MediaElement"/> so that
/// <c>AttachAndRun</c> creates it, or create it directly with <c>CreateHandler&lt;AndroidMediaElementProbeHandler&gt;</c>.
/// </summary>
public sealed class AndroidMediaElementProbeHandler : MediaElementHandler
{
	/// <summary>The manager the handler created for the virtual view (null once the handler was disposed).</summary>
	public MediaManager? Manager => MediaManager;

	/// <summary>Runs the protected <c>CreatePlatformView</c> again, or for the first time on a handler that has no MauiContext.</summary>
	public MauiMediaElement CreatePlatformViewDirectly() => CreatePlatformView();

	/// <summary>Runs the protected <c>DisconnectHandler(MauiMediaElement)</c> of the toolkit directly, without MAUI's guard around it.</summary>
	public void DisconnectPlatformView(MauiMediaElement platformView) => DisconnectHandler(platformView);

	/// <summary>Runs the protected <c>Dispose(bool)</c>, which is how the finalizer path (<c>disposing: false</c>) is reached.</summary>
	public void InvokeDispose(bool disposing) => Dispose(disposing);
}

/// <summary>
/// A <see cref="MediaManager"/> created by a test (not by the handler), with access to its protected members.
/// </summary>
public sealed class AndroidMediaManagerProbe(IMauiContext context, IMediaElement mediaElement, IDispatcher dispatcher) : MediaManager(context, mediaElement, dispatcher)
{
	public PlayerView? ExposedPlayerView => PlayerView;

	public IExoPlayer? ExposedPlayer => Player;

	/// <summary>Gives the manager a player without a PlayerView (what a subclass can do): CreatePlatformView is not run.</summary>
	public void AttachPlayer(IExoPlayer player) => Player = player;

	public void InvokeDispose(bool disposing) => Dispose(disposing);
}