namespace CommunityToolkit.Maui.Primitives;
class NotificationEventArgs(string action, string sender) : EventArgs
{
	public string Sender { get; set; } = sender;
	public string Action { get; set; } = action;
}
