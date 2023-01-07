namespace CommunityToolkit.Maui.MediaView;

/// <summary>
/// Handler class for <see cref="MediaView"/>.
/// </summary>
public partial class MediaViewHandler
{
#if ANDROID || IOS || MACCATALYST || WINDOWS || TIZEN
	MediaManager? mediaManager;
#endif

	/// <summary>
	/// The default property mapper for this handler.
	/// </summary>
	public static IPropertyMapper<MediaView, MediaViewHandler> PropertyMapper = new PropertyMapper<MediaView, MediaViewHandler>(ViewMapper)
	{
		[nameof(IMediaView.ShouldShowPlaybackControls)] = MapShouldShowPlaybackControls,
		[nameof(IMediaView.Source)] = MapSource,
		[nameof(IMediaView.Speed)] = MapSpeed,
		[nameof(IMediaView.Volume)] = MapVolume,
		[nameof(IMediaView.ShouldKeepScreenOn)] = MapShouldKeepScreenOn,
#if ANDROID || WINDOWS || TIZEN
		[nameof(MediaView.ShouldLoopPlayback)] = ShouldLoopPlayback
#endif
	};

	/// <summary>
	/// The default command mapper for this handler.
	/// </summary>
	public static CommandMapper<MediaView, MediaViewHandler> CommandMapper = new(ViewCommandMapper)
	{
		[nameof(MediaView.StatusUpdated)] = MapStatusUpdated,
		[nameof(MediaView.PlayRequested)] = MapPlayRequested,
		[nameof(MediaView.PauseRequested)] = MapPauseRequested,
		[nameof(MediaView.SeekRequested)] = MapSeekRequested,
		[nameof(MediaView.StopRequested)] = MapStopRequested
	};

	/// <summary>
	/// Initializes a new instance of the <see cref="MediaViewHandler"/> class.
	/// </summary>
	public MediaViewHandler() : base(PropertyMapper, CommandMapper)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MediaViewHandler"/> class
	/// with custom property and command mappers.
	/// </summary>
	/// <param name="mapper">The custom property mapper to use.</param>
	/// <param name="commandMapper">The custom command mapper to use.</param>
	public MediaViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? PropertyMapper, commandMapper ?? CommandMapper)
	{

	}

#if ANDROID || IOS || MACCATALYST || WINDOWS || TIZEN
	/// <summary>
	/// Maps the <see cref="IMediaView.ShouldShowPlaybackControls"/> property between the abstract
	/// <see cref="MediaView"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaView">The associated <see cref="MediaView"/> instance.</param>
	public static void MapShouldShowPlaybackControls(MediaViewHandler handler, MediaView MediaView)
	{
		handler.mediaManager?.UpdateShouldShowPlaybackControls();
	}

	/// <summary>
	/// Maps the <see cref="IMediaView.Source"/> property between the abstract
	/// <see cref="MediaView"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaView">The associated <see cref="MediaView"/> instance.</param>
	public static void MapSource(MediaViewHandler handler, MediaView MediaView)
	{
		handler.mediaManager?.UpdateSource();
	}

	/// <summary>
	/// Maps the <see cref="IMediaView.Speed"/> property between the abstract
	/// <see cref="MediaView"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaView">The associated <see cref="MediaView"/> instance.</param>
	public static void MapSpeed(MediaViewHandler handler, MediaView MediaView)
	{
		handler.mediaManager?.UpdateSpeed();
	}

	/// <summary>
	/// Maps the status update between the abstract <see cref="MediaView"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaView">The associated <see cref="MediaView"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapStatusUpdated(MediaViewHandler handler, MediaView MediaView, object? args)
	{
		handler.mediaManager?.UpdateStatus();
	}

	/// <summary>
	/// Maps the <see cref="IMediaView.Volume"/> property between the abstract
	/// <see cref="MediaView"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaView">The associated <see cref="MediaView"/> instance.</param>
	public static void MapVolume(MediaViewHandler handler, MediaView MediaView)
	{
		handler.mediaManager?.UpdateVolume();
	}

	/// <summary>
	/// Maps the <see cref="IMediaView.ShouldKeepScreenOn"/> property between the abstract
	/// <see cref="MediaView"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaView">The associated <see cref="MediaView"/> instance.</param>
	public static void MapShouldKeepScreenOn(MediaViewHandler handler, MediaView MediaView)
	{
		handler.mediaManager?.UpdateShouldKeepScreenOn();
	}

	/// <summary>
	/// Maps the play operation request between the abstract <see cref="MediaView"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaView">The associated <see cref="MediaView"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapPlayRequested(MediaViewHandler handler, MediaView MediaView, object? args)
	{
		handler.mediaManager?.Play();
	}

	/// <summary>
	/// Maps the pause operation request between the abstract <see cref="MediaView"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaView">The associated <see cref="MediaView"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapPauseRequested(MediaViewHandler handler, MediaView MediaView, object? args)
	{
		handler.mediaManager?.Pause();
	}

	/// <summary>
	/// Maps the seek operation request between the abstract <see cref="MediaView"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaView">The associated <see cref="MediaView"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> should be of type <see cref="MediaSeekRequestedEventArgs"/>, otherwise nothing happens.</remarks>
	public static void MapSeekRequested(MediaViewHandler handler, MediaView MediaView, object? args)
	{
		ArgumentNullException.ThrowIfNull(args);

		var positionArgs = (MediaSeekRequestedEventArgs)args;
		handler.mediaManager?.Seek(positionArgs.RequestedPosition);
	}

	/// <summary>
	/// Maps the stop operation request between the abstract <see cref="MediaView"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaView">The associated <see cref="MediaView"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapStopRequested(MediaViewHandler handler, MediaView MediaView, object? args)
	{
		handler.mediaManager?.Stop();
	}

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MediaView"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="MediaView"/> and optionally releases the managed resources.
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