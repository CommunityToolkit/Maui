using CommunityToolkit.Maui.Primitives;

namespace CommunityToolkit.Maui.Services;
class NotificationService
{
	public event EventHandler<NotificationEventArgs>? NotificationReceived;
	public static NotificationService? Instance { get;} = new();

	public void Received(string action, string sender)
	{
		var args = new NotificationEventArgs(action, sender);
		NotificationReceived?.Invoke(this, args);
	}
}
