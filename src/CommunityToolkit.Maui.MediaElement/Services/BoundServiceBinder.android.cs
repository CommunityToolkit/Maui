using Android.OS;
using CommunityToolkit.Maui.Media.Services;

namespace CommunityToolkit.Maui.Services;

sealed class BoundServiceBinder(MediaControlsService mediaControlsService) : Binder
{
	public MediaControlsService Service { get; } = mediaControlsService;
}