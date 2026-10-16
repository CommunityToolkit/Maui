using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ValidationBehaviorTests
{
	static Color? GetPlatformTextColor(IPlatformViewHandler handler)
	{
		var textColor = ((UITextField)handler.PlatformView!).TextColor;

		if (textColor is null)
		{
			return null;
		}

		textColor.GetRGBA(out var red, out var green, out var blue, out var alpha);

		return new Color((float)red, (float)green, (float)blue, (float)alpha);
	}
}