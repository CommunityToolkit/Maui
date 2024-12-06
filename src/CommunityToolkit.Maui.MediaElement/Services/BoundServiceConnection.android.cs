using Android.Content;
using Android.OS;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Services;
sealed partial class BoundServiceConnection(MediaManager mediaManager) : Java.Lang.Object, IServiceConnection
{
	public MediaManager? Activity { get; } = mediaManager;

	public bool IsConnected => isConnected;
	bool isConnected = false;

	public BoundServiceBinder? Binder = null;

	void IServiceConnection.OnServiceConnected(ComponentName? name, IBinder? service)
	{
		Binder = service as BoundServiceBinder;
		isConnected = Binder is not null;
		// UpdateNotifications needs to be called as it may have been called before the service was connected
		Activity?.UpdateNotifications();
	}

	void IServiceConnection.OnServiceDisconnected(ComponentName? name)
	{
		isConnected = false;
		Binder = null;
	}
}
