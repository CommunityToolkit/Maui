using Microsoft.UI.Xaml.Controls;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ValidationBehaviorTests
{
	static Color? GetPlatformTextColor(IPlatformViewHandler handler) =>
		((TextBox)handler.PlatformView!).Foreground is WSolidColorBrush brush
			? Color.FromRgba(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A)
			: null;
}