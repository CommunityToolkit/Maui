using Android.OS;
using CommunityToolkit.Maui.Media.Services;

namespace CommunityToolkit.Maui.Services;

class BoundServiceBinder(MediaControlsService mediaControlsService) : Binder
{
	public MediaControlsService? Service { get; private set; } = mediaControlsService;
}
