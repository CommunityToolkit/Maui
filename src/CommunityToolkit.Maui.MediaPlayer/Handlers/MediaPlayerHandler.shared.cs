namespace CommunityToolkit.Maui.MediaPlayer;

/// <summary>
/// Handler class for <see cref="MediaPlayer"/>.
/// </summary>
public partial class MediaPlayerHandler
{
#if ANDROID || IOS || MACCATALYST || WINDOWS || TIZEN
	MediaManager? mediaManager;
#endif

	/// <summary>
	/// The default property mapper for this handler.
	/// </summary>
	public static IPropertyMapper<Views.MediaPlayer, MediaPlayerHandler> PropertyMapper = new PropertyMapper<Views.MediaPlayer, MediaPlayerHandler>(ViewMapper)
	{
		[nameof(Core.IMediaPlayer.ShouldShowPlaybackControls)] = MapShouldShowPlaybackControls,
		[nameof(Core.IMediaPlayer.Source)] = MapSource,
		[nameof(Core.IMediaPlayer.Speed)] = MapSpeed,
		[nameof(Core.IMediaPlayer.Volume)] = MapVolume,
		[nameof(Core.IMediaPlayer.ShouldKeepScreenOn)] = MapShouldKeepScreenOn,
#if ANDROID || WINDOWS || TIZEN
		[nameof(Core.IMediaPlayer.ShouldLoopPlayback)] = ShouldLoopPlayback
#endif
	};

	/// <summary>
	/// The default command mapper for this handler.
	/// </summary>
	public static CommandMapper<Views.MediaPlayer, MediaPlayerHandler> CommandMapper = new(ViewCommandMapper)
	{
		[nameof(Views.MediaPlayer.StatusUpdated)] = MapStatusUpdated,
		[nameof(Views.MediaPlayer.PlayRequested)] = MapPlayRequested,
		[nameof(Views.MediaPlayer.PauseRequested)] = MapPauseRequested,
		[nameof(Views.MediaPlayer.SeekRequested)] = MapSeekRequested,
		[nameof(Views.MediaPlayer.StopRequested)] = MapStopRequested
	};

	/// <summary>
	/// Initializes a new instance of the <see cref="MediaPlayerHandler"/> class.
	/// </summary>
	public MediaPlayerHandler() : base(PropertyMapper, CommandMapper)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MediaPlayerHandler"/> class
	/// with custom property and command mappers.
	/// </summary>
	/// <param name="mapper">The custom property mapper to use.</param>
	/// <param name="commandMapper">The custom command mapper to use.</param>
	public MediaPlayerHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? PropertyMapper, commandMapper ?? CommandMapper)
	{

	}

#if ANDROID || IOS || MACCATALYST || WINDOWS || TIZEN
	/// <summary>
	/// Maps the <see cref="Core.IMediaPlayer.ShouldShowPlaybackControls"/> property between the abstract
	/// <see cref="Views.MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="Views.MediaPlayer"/> instance.</param>
	public static void MapShouldShowPlaybackControls(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer)
	{
		handler.mediaManager?.UpdateShouldShowPlaybackControls();
	}

	/// <summary>
	/// Maps the <see cref="Core.IMediaPlayer.Source"/> property between the abstract
	/// <see cref="MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="MediaPlayer"/> instance.</param>
	public static void MapSource(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer)
	{
		handler.mediaManager?.UpdateSource();
	}

	/// <summary>
	/// Maps the <see cref="Core.IMediaPlayer.Speed"/> property between the abstract
	/// <see cref="Views.MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="Views.MediaPlayer"/> instance.</param>
	public static void MapSpeed(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer)
	{
		handler.mediaManager?.UpdateSpeed();
	}

	/// <summary>
	/// Maps the status update between the abstract <see cref="MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="MediaPlayer"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapStatusUpdated(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer, object? args)
	{
		handler.mediaManager?.UpdateStatus();
	}

	/// <summary>
	/// Maps the <see cref="Core.IMediaPlayer.Volume"/> property between the abstract
	/// <see cref="MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="MediaPlayer"/> instance.</param>
	public static void MapVolume(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer)
	{
		handler.mediaManager?.UpdateVolume();
	}

	/// <summary>
	/// Maps the <see cref="Core.IMediaPlayer.ShouldKeepScreenOn"/> property between the abstract
	/// <see cref="MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="MediaPlayer"/> instance.</param>
	public static void MapShouldKeepScreenOn(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer)
	{
		handler.mediaManager?.UpdateShouldKeepScreenOn();
	}

	/// <summary>
	/// Maps the play operation request between the abstract <see cref="MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="MediaPlayer"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapPlayRequested(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer, object? args)
	{
		handler.mediaManager?.Play();
	}

	/// <summary>
	/// Maps the pause operation request between the abstract <see cref="MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="MediaPlayer"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapPauseRequested(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer, object? args)
	{
		handler.mediaManager?.Pause();
	}

	/// <summary>
	/// Maps the seek operation request between the abstract <see cref="MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="MediaPlayer"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> should be of type <see cref="MediaSeekRequestedEventArgs"/>, otherwise nothing happens.</remarks>
	public static void MapSeekRequested(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer, object? args)
	{
		ArgumentNullException.ThrowIfNull(args);

		var positionArgs = (MediaSeekRequestedEventArgs)args;
		handler.mediaManager?.Seek(positionArgs.RequestedPosition);
	}

	/// <summary>
	/// Maps the stop operation request between the abstract <see cref="MediaPlayer"/> and platform counterpart.
	/// </summary>
	/// <param name="handler">The associated handler.</param>
	/// <param name="MediaPlayer">The associated <see cref="MediaPlayer"/> instance.</param>
	/// <param name="args">The associated event arguments for this request.</param>
	/// <remarks><paramref name="args"/> is not used.</remarks>
	public static void MapStopRequested(MediaPlayerHandler handler, Views.MediaPlayer MediaPlayer, object? args)
	{
		handler.mediaManager?.Stop();
	}

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="Views.MediaPlayer"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="Views.MediaPlayer"/> and optionally releases the managed resources.
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