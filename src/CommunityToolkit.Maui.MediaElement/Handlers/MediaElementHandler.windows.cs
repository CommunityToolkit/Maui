using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
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
		handler.mediaManager?.UpdateShouldLoopPlayback();
	}

	/// <inheritdoc/>
	protected override MauiMediaElement CreatePlatformView()
	{
		mediaManager ??= new(MauiContext ?? throw new NullReferenceException(),
								VirtualView,
								Dispatcher.GetForCurrentThread() ?? throw new InvalidOperationException($"{nameof(IDispatcher)} cannot be null"));

		var mediaPlatform = mediaManager.CreatePlatformView();
		return new(mediaPlatform);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		Dispose();
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