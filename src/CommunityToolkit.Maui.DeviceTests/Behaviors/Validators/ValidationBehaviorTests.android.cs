using Android.Widget;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ValidationBehaviorTests
{
	static Color? GetPlatformTextColor(IPlatformViewHandler handler) => Color.FromInt(((EditText)handler.PlatformView!).CurrentTextColor);
}