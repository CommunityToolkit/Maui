namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler
{
	MediaManager? mediaManager;
	public static IPropertyMapper<MediaElement, MediaElementHandler> PropertyMapper = new PropertyMapper<MediaElement, MediaElementHandler>(ViewMapper)
	{
		[nameof(IMediaElement.ShowsPlaybackControls)] = MapShowsPlaybackControls,
		[nameof(IMediaElement.Source)] = MapSource,
		[nameof(IMediaElement.Speed)] = MapSpeed,
		[nameof(IMediaElement.Volume)] = MapVolume,
#if __ANDROID__ || __IOS__ || __MACCATALYST__
		[nameof(IMediaElement.KeepScreenOn)] = MapKeepScreenOn,
#endif
#if __ANDROID__ || WINDOWS
		[nameof(MediaElement.IsLooping)] = MapIsLooping
#endif
	};

	public static CommandMapper<MediaElement, MediaElementHandler> CommandMapper = new(ViewCommandMapper)
	{
		[nameof(MediaElement.UpdateStatus)] = MapUpdateStatus,
		[nameof(MediaElement.PlayRequested)] = MapPlayRequested,
		[nameof(MediaElement.PauseRequested)] = MapPauseRequested,
		[nameof(MediaElement.SeekRequested)] = MapSeekRequested,
		[nameof(MediaElement.StopRequested)] = MapStopRequested
	};

	public MediaElementHandler() : base(PropertyMapper, CommandMapper)
	{
	}

	public MediaElementHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? PropertyMapper, commandMapper ?? CommandMapper)
	{

	}
}