using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>, IDisposable
{
	/// <summary>
	/// Maps the <see cref="Core.IMediaElement.ShouldLoopPlayback"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void ShouldLoopPlayback(MediaElementHandler handler, MediaElement MediaElement)
	{
		handler?.MediaManager?.UpdateShouldLoopPlayback();
	}

	/// <inheritdoc/>
	protected override MauiMediaElement CreatePlatformView()
	{
		MediaManager ??= new(MauiContext ?? throw new NullReferenceException(),
								VirtualView,
								Dispatcher.GetForCurrentThread() ?? throw new InvalidOperationException($"{nameof(IDispatcher)} cannot be null"));

		var mediaPlatform = MediaManager.CreatePlatformView();
		
		return new(mediaPlatform);
	}

	/// <summary>
	/// Establishes a connection between the handler and the specified platform-specific media element view.
	/// </summary>
	/// <remarks>Overrides the base implementation to provide custom connection logic for the media element on the
	/// Windows platform.</remarks>
	/// <param name="platformView">The platform-specific media element view to connect to the handler. Cannot be null.</param>
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
	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		Dispose();
		platformView.FullScreenStateChanged -= OnScreenStateChanged;
		UnloadPlatformView(platformView);
		base.DisconnectHandler(platformView);
	}

	static void UnloadPlatformView(MauiMediaElement platformView)
	{
		if (platformView.IsLoaded)
		{
			platformView.Unloaded += OnPlatformViewUnloaded;
		}
		else
		{
			platformView.Dispose();
		}

		static void OnPlatformViewUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			var mediaElement = (MauiMediaElement)sender;

			mediaElement.Unloaded -= OnPlatformViewUnloaded;
			mediaElement.Dispose();
		}
	}
}