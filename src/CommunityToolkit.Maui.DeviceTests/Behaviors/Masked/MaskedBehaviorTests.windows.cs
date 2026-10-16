using Microsoft.UI.Xaml.Controls;
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows: an Entry is a TextBox, whose text changes raise its TextChanged event, from which .NET MAUI updates Entry.Text.
public partial class MaskedBehaviorTests
{
	static void SetPlatformText(PlatformView platformView, string text) => ((TextBox)platformView).Text = text;

	static string? GetPlatformText(PlatformView platformView) => ((TextBox)platformView).Text;
}