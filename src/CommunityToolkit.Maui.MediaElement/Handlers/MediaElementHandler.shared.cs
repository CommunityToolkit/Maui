using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler
{
	public static IPropertyMapper<MediaElement, MediaElementHandler> PropertyMapper = new PropertyMapper<MediaElement, MediaElementHandler>(ViewMapper)
	{
		[nameof(IMediaElement.Source)] = MapSource,
	};

	public static CommandMapper<MediaElement, MediaElementHandler> CommandMapper = new(ViewCommandMapper)
	{
		// TODO
	};

	public MediaElementHandler() : base(PropertyMapper, CommandMapper)
	{
	}

	public MediaElementHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? PropertyMapper, commandMapper ?? CommandMapper)
	{

	}
}
