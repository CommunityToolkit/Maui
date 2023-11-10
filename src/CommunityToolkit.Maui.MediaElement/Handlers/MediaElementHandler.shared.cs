using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// Handler class for <see cref="MediaElement"/>.
/// </summary>
public partial class MediaElementHandler
{
#if ANDROID || IOS || MACCATALYST || WINDOWS || TIZEN
	/// <summary>
	/// The <see cref="MediaManager"/> that is managing the <see cref="IMediaElement"/> instance.
	/// </summary>
	protected MediaManager? mediaManager;
#endif

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
	/// Maps the <see cref="IMediaElement.Aspect"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapAspect(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.mediaManager?.UpdateAspect();
	}

	/// <summary>
	/// Maps the <see cref="IMediaElement.ShouldShowPlaybackControls"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapShouldShowPlaybackControls(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.mediaManager?.UpdateShouldShowPlaybackControls();
	}

	/// <summary>
	/// Maps the <see cref="IMediaElement.Source"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapSource(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.mediaManager?.UpdateSource();
	}

	/// <summary>
	/// Maps the <see cref="Core.IMediaElement.Speed"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapSpeed(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.mediaManager?.UpdateSpeed();
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
		handler.mediaManager?.UpdateStatus();
	}

	/// <summary>
	/// Maps the <see cref="Core.IMediaElement.Volume"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapVolume(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.mediaManager?.UpdateVolume();
	}

	/// <summary>
	/// Maps the <see cref="Core.IMediaElement.ShouldKeepScreenOn"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapShouldKeepScreenOn(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.mediaManager?.UpdateShouldKeepScreenOn();
	}

	/// <summary>
	/// Maps the <see cref="Core.IMediaElement.ShouldMute"/> property between the abstract
	/// <see cref="MediaElement"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="mediaElement">The associated <see cref="MediaElement"/> instance.</param>
	public static void MapShouldMute(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler.mediaManager?.UpdateShouldMute();
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
		handler.mediaManager?.Play();
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
		handler.mediaManager?.Pause();
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

		var positionArgs = (MediaSeekRequestedEventArgs)args;
		await (handler.mediaManager?.Seek(positionArgs.RequestedPosition, CancellationToken.None) ?? Task.CompletedTask);

		((IMediaElement)mediaElement).SeekCompletedTCS.TrySetResult();
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
		handler.mediaManager?.Stop();
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
			mediaManager?.Dispose();
			mediaManager = null;
		}
	}
#endif
}