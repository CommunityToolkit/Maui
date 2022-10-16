using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler
{
	public static IPropertyMapper<MediaElement, MediaElementHandler> PropertyMapper = new PropertyMapper<MediaElement, MediaElementHandler>(ViewMapper)
	{
		[nameof(IMediaElement.ShowsPlaybackControls)] = MapShowsPlaybackControls,
		[nameof(IMediaElement.Source)] = MapSource,
		[nameof(IMediaElement.Speed)] = MapSpeed,
		[nameof(IMediaElement.Volume)] = MapVolume,
#if __IOS__
		[nameof(IMediaElement.Position)] = MapPosition
#endif
	};

	public static CommandMapper<MediaElement, MediaElementHandler> CommandMapper = new(ViewCommandMapper)
	{
		// TODO
#if __IOS__
		[nameof(MediaElement.UpdateStatus)] = MapUpdateStatus
#endif
	};

	public MediaElementHandler() : base(PropertyMapper, CommandMapper)
	{
	}

	public MediaElementHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? PropertyMapper, commandMapper ?? CommandMapper)
	{

	}
}
