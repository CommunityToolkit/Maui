using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// Handler class for <see cref="MediaElement"/>.
/// </summary>
public partial class MediaElementHandler
{
	/// <summary>
	/// The default property mapper for this handler.
	/// </summary>
	public static IPropertyMapper<MediaElement, MediaElementHandler> PropertyMapper = new PropertyMapper<MediaElement, MediaElementHandler>(ViewMapper)
	{
		[nameof(IMediaElement.Aspect)] = MapAspect,
		[nameof(IMediaElement.ShouldShowPlaybackControls)] = MapShouldShowPlaybackControls,
		[nameof(IMediaElement.Source)] = MapSource,
		[nameof(IMediaElement.Speed)] = MapSpeed,
		[nameof(IMediaElement.Volume)] = MapVolume,
		[nameof(IMediaElement.ShouldKeepScreenOn)] = MapShouldKeepScreenOn,
		[nameof(IMediaElement.ShouldMute)] = MapShouldMute,
#if ANDROID || WINDOWS || TIZEN
		[nameof(IMediaElement.ShouldLoopPlayback)] = ShouldLoopPlayback
#endif
	};

	/// <summary>
	/// The default command mapper for this handler.
	/// </summary>
	public static CommandMapper<MediaElement, MediaElementHandler> CommandMapper = new(ViewCommandMapper)
	{
		[nameof(MediaElement.StatusUpdated)] = MapStatusUpdated,
		[nameof(MediaElement.PlayRequested)] = MapPlayRequested,
		[nameof(MediaElement.PauseRequested)] = MapPauseRequested,
		[nameof(MediaElement.SeekRequested)] = MapSeekRequested,
		[nameof(MediaElement.StopRequested)] = MapStopRequested
	};

	/// <summary>
	/// Initializes a new instance of the <see cref="MediaElementHandler"/> class.
	/// </summary>
	public MediaElementHandler() : base(PropertyMapper, CommandMapper)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MediaElementHandler"/> class
	/// with custom property and command mappers.
	/// </summary>
	/// <param name="mapper">The custom property mapper to use.</param>
	/// <param name="commandMapper">The custom command mapper to use.</param>
	public MediaElementHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? PropertyMapper, commandMapper ?? CommandMapper)
	{

	}

#if ANDROID || IOS || MACCATALYST || WINDOWS || TIZEN
	/// <summary>
	/// The <see cref="Views.MediaManager"/> that is managing the <see cref="IMediaElement"/> instance.
	/// </summary>
	
	protected MediaManager? MediaManager { get; set; }
	
	/// <summary>
	/// Maps the <see cref="IMediaElement.Aspect"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapAspect(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.MediaManager?.UpdateAspect();
	}

	/// <summary>
	/// Maps the <see cref="IMediaElement.ShouldShowPlaybackControls"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapShouldShowPlaybackControls(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.MediaManager?.UpdateShouldShowPlaybackControls();
		handler.PlatformRefreshPlaybackControlsVisibility(mediaElement.ShouldShowPlaybackControls);
	}

	/// <summary>
	/// Maps the <see cref="IMediaElement.Source"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	/// <remarks>
	/// The platform's <see cref="Views.MediaManager"/> reports a source that it cannot apply through <see cref="MediaElement.MediaFailed"/>. An
	/// exception thrown while the source is updated, such as the exception of an event handler that the update raises, propagates: to the caller
	/// while the update runs synchronously, and on the dispatcher once the update continues asynchronously.
	/// </remarks>
	public static void MapSource(MediaElementHandler handler, MediaElement mediaElement)
	{
		if (handler.MediaManager is not null)
		{
			UpdateSource(handler.MediaManager.UpdateSource());
		}
	}

	partial void PlatformRefreshPlaybackControlsVisibility(bool shouldShowPlaybackControls);

	/// <summary>
	/// Maps the <see cref="Core.IMediaElement.Speed"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapSpeed(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.MediaManager?.UpdateSpeed();
	}

	/// <summary>
	/// Maps the status update between the abstract <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapStatusUpdated(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		handler.MediaManager?.UpdateStatus();
	}

	/// <summary>
	/// Maps the <see cref="Core.IMediaElement.Volume"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapVolume(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.MediaManager?.UpdateVolume();
	}

	/// <summary>
	/// Maps the <see cref="Core.IMediaElement.ShouldKeepScreenOn"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapShouldKeepScreenOn(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.MediaManager?.UpdateShouldKeepScreenOn();
	}

	/// <summary>
	/// Maps the <see cref="Core.IMediaElement.ShouldMute"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapShouldMute(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.MediaManager?.UpdateShouldMute();
	}

	/// <summary>
	/// Maps the play operation request between the abstract <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapPlayRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		handler.MediaManager?.Play();
	}

	/// <summary>
	/// Maps the pause operation request between the abstract <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapPauseRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		handler.MediaManager?.Pause();
	}

	/// <summary>
	/// Maps the seek operation request between the abstract <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> should be of type <see cref="MediaSeekRequestedEventArgs"/>, otherwise nothing happens.</remarks>
	public static async void MapSeekRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		ArgumentNullException.ThrowIfNull(args);

		if (args is not MediaSeekRequestedEventArgs positionArgs)
		{
			return;
		}

		// Captured before the await: a SeekTo whose token ends its wait cancels this task (so the later result or failure of this seek changes
		// nothing) and replaces it for the next request
		var seekCompletedTaskCompletionSource = ((IMediaElement)mediaElement).SeekCompletedTCS;

		try
		{
			await (handler.MediaManager?.Seek(positionArgs.RequestedPosition, CancellationToken.None) ?? Task.CompletedTask);
			seekCompletedTaskCompletionSource.TrySetResult();
		}
		catch (Exception ex)
		{
			// A seek that fails (for example before the player is ready) fails SeekTo: thrown from this async void method it would crash the app,
			// and SeekTo would wait for this seek forever
			seekCompletedTaskCompletionSource.TrySetException(ex);
		}
	}

	/// <summary>
	/// Maps the stop operation request between the abstract <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapStopRequested(MediaElementHandler handler, MediaElement mediaElement, object? args)
	{
		handler.MediaManager?.Stop();
	}

	// A property mapper cannot await MediaManager.UpdateSource. An exception of the update is not a failure of the source (those go through
	// MediaFailed) but typically one that an event handler raised by the update threw, so it propagates like one: to the code that set the Source
	// while the update runs synchronously (the whole update on iOS and Mac Catalyst), and on the dispatcher once the update has awaited.
	// An update that a newer one replaced (OperationCanceledException) has nothing left to report.
	static void UpdateSource(ValueTask update)
	{
		if (update.IsCompleted)
		{
			update.GetAwaiter().GetResult();
			return;
		}

		AwaitSourceUpdate(update);

		static async void AwaitSourceUpdate(ValueTask update)
		{
			try
			{
				await update;
			}
			catch (OperationCanceledException)
			{
			}
		}
	}

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MediaElement"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="MediaElement"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			MediaManager?.Dispose();
			MediaManager = null;
			PlatformDispose();
		}
	}

	partial void PlatformDispose();
#endif
}