using Android.Widget;
using PlatformView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class MaskedBehaviorTests
{
	static void SetPlatformText(PlatformView platformView, string text) => ((EditText)platformView).Text = text;

	static string? GetPlatformText(PlatformView platformView) => ((EditText)platformView).Text;
}