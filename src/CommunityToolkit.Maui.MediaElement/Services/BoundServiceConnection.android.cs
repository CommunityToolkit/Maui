using Android.Content;
using Android.OS;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Services;

sealed partial class BoundServiceConnection(MediaManager mediaManager) : Java.Lang.Object, IServiceConnection
{
	readonly WeakEventManager taskRemovedEventManager = new();

	public event EventHandler MediaControlsServiceTaskRemoved
	{
		add => taskRemovedEventManager.AddEventHandler(value);
		remove => taskRemovedEventManager.RemoveEventHandler(value);
	}

	public MediaManager? Activity { get; } = mediaManager;

	public bool IsConnected => Binder is not null;

	public BoundServiceBinder? Binder { get; private set; }

	void HandleTaskRemoved(object? sender, EventArgs e)
	{
		taskRemovedEventManager.HandleEvent(this, EventArgs.Empty, nameof(MediaControlsServiceTaskRemoved));
	}

	void IServiceConnection.OnServiceConnected(ComponentName? name, IBinder? service)
	{
		Binder = service as BoundServiceBinder;

		if (Binder is not null)
		{
			Binder.Service.TaskRemoved += HandleTaskRemoved;
		}

		// UpdateNotifications needs to be called as it may have been called before the service was connected
		Activity?.UpdateNotifications();
	}

	void IServiceConnection.OnServiceDisconnected(ComponentName? name)
	{
		if (Binder is not null)
		{
			Binder.Service.TaskRemoved -= HandleTaskRemoved;
			Binder = null;
		}
	}
}