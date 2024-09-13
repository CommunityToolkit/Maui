using CommunityToolkit.Maui.Interfaces;
using CommunityToolkit.Maui.Primitives;

namespace CommunityToolkit.Maui.Services;
class NotificationService : INotificationService
{
	public event EventHandler<NotificationEventArgs>? NotificationReceived;
	public static NotificationService? Instance { get; private set; }

	public NotificationService()
	{
		Instance ??= this;
	}
	public void Received(string action, string sender)
	{
		var args = new NotificationEventArgs(action, sender);
		NotificationReceived?.Invoke(this, args);
	}
}
