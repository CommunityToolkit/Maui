
using AndroidX.Media3.UI;

namespace CommunityToolkit.Maui.Primitives;
public class NotificationEventArgs(PlayerView? playerview) : EventArgs
{
	public PlayerView? Playerview { get; } = playerview;
}
