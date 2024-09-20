using Android.App;
using Android.OS;
using CommunityToolkit.Maui.Media.Services;

namespace CommunityToolkit.Maui.Services;
class BoundServiceBinder : Binder
{
    public MediaControlsService? Service { get; private set; } = null;

    public BoundServiceBinder(MediaControlsService mediaControlsService)
    {
        Service = mediaControlsService;
    }
}
