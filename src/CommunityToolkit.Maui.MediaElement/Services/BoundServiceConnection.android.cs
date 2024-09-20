using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Media.Services;

namespace CommunityToolkit.Maui.Services;
class BoundServiceConnection : Java.Lang.Object, IServiceConnection
{
    public MediaManager? activity { get; private set; }

    public BoundServiceConnection(MediaManager mediaManager)
    {
        IsConnected = false;
        Binder = null;
        this.activity = mediaManager;
    }

    public bool IsConnected { get; private set; }
    public BoundServiceBinder? Binder { get; private set; }
    void IServiceConnection.OnServiceConnected(ComponentName? name, IBinder? service)
    {
        Binder = service as BoundServiceBinder;
        IsConnected = this.Binder != null;
		activity?.UpdatePlayer();
	}
    void IServiceConnection.OnServiceDisconnected(ComponentName? name)
    {
        IsConnected = false;
        Binder = null;
    }
}
