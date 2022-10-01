using System;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler : ViewHandler<MediaElement, object>
{
	protected override object CreatePlatformView() => throw new NotImplementedException();

	public static void MapSource(object handler, MediaElement mediaElement) { }
}