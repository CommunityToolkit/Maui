using Android.Widget;
using PlatformView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// Android: an Entry is an EditText, whose text changes raise its TextChanged event, from which .NET MAUI updates Entry.Text.
public partial class MaskedBehaviorTests
{
	static void SetPlatformText(PlatformView platformView, string text) => ((EditText)platformView).Text = text;

	static string? GetPlatformText(PlatformView platformView) => ((EditText)platformView).Text;
}