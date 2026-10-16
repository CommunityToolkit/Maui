using Microsoft.UI.Xaml.Controls;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows: the platform view of an Entry is a TextBox; MAUI gives it the TextColor of the Entry as its Foreground brush.
public partial class ValidationBehaviorTests
{
	static Color? GetPlatformTextColor(IPlatformViewHandler handler) =>
		((TextBox)handler.PlatformView!).Foreground is WSolidColorBrush brush
			? Color.FromRgba(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A)
			: null;
}