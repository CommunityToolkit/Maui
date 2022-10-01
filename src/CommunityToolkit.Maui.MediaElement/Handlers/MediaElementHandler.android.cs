using CommunityToolkit.Maui.MediaElement.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>
{
	protected override MauiMediaElement CreatePlatformView() => new(Context, VirtualView);

	public static void MapSource(MediaElementHandler handler, MediaElement mediaElement)
	{
		handler?.PlatformView.UpdateSource();
	}
}

