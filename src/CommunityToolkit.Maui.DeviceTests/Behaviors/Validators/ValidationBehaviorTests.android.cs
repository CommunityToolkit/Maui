using Android.Widget;

namespace CommunityToolkit.Maui.DeviceTests;

// Android: the platform view of an Entry is an EditText; MAUI gives it the TextColor of the Entry with TextView.SetTextColor.
public partial class ValidationBehaviorTests
{
	static Color? GetPlatformTextColor(IPlatformViewHandler handler) => Color.FromInt(((EditText)handler.PlatformView!).CurrentTextColor);
}