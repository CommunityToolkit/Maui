using Android.Content;
using Android.OS;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Services;
class BoundServiceConnection(MediaManager mediaManager) : Java.Lang.Object, IServiceConnection
{
    public MediaManager? Activity { get; private set; } = mediaManager;

	public bool IsConnected { get; private set; } = false;

	public BoundServiceBinder? Binder { get; private set; } = null;

	void IServiceConnection.OnServiceConnected(ComponentName? name, IBinder? service)
    {
        Binder = service as BoundServiceBinder;
        IsConnected = Binder is not null;
		Activity?.UpdatePlayer();
	}

    void IServiceConnection.OnServiceDisconnected(ComponentName? name)
    {
        IsConnected = false;
        Binder = null;
    }
}
