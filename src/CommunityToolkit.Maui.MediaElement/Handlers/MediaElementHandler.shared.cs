﻿namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler
{
	public static IPropertyMapper<MediaElement, MediaElementHandler> PropertyMapper = new PropertyMapper<MediaElement, MediaElementHandler>(ViewMapper)
	{
		[nameof(IMediaElement.ShowsPlaybackControls)] = MapShowsPlaybackControls,
		[nameof(IMediaElement.Source)] = MapSource,
		[nameof(IMediaElement.Speed)] = MapSpeed,
		[nameof(IMediaElement.Volume)] = MapVolume,
		[nameof(IMediaElement.Position)] = MapPosition,
#if __ANDROID__
		[nameof(MediaElement.IsLooping)] = MapIsLooping
#endif
	};

	public static CommandMapper<MediaElement, MediaElementHandler> CommandMapper = new(ViewCommandMapper)
	{
		[nameof(MediaElement.UpdateStatus)] = MapUpdateStatus,
		[nameof(MediaElement.PlayRequested)] = MapPlayRequested,
		[nameof(MediaElement.PauseRequested)] = MapPauseRequested,
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