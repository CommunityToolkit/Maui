using Android.Content;
using Android.OS;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Services;
class BoundServiceConnection(MediaManager mediaManager) : Java.Lang.Object, IServiceConnection
{
    public MediaManager? Activity { get; } = mediaManager;

	public bool IsConnected => isConnected;
	bool isConnected = false;

	public BoundServiceBinder? Binder = null;

	void IServiceConnection.OnServiceConnected(ComponentName? name, IBinder? service)
    {
        Binder = service as BoundServiceBinder;
        isConnected = Binder is not null;
		Activity?.UpdatePlayer();
	}

    void IServiceConnection.OnServiceDisconnected(ComponentName? name)
    {
        isConnected = false;
        Binder = null;
    }
}
