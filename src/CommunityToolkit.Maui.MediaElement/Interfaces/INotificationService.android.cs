using CommunityToolkit.Maui.Primitives;

namespace CommunityToolkit.Maui.Interfaces;
interface INotificationService
{
	event EventHandler<NotificationEventArgs>? NotificationReceived;
	void Received(string action, string sender);
}
