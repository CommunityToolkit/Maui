using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class KeyboardDismissalTestBase
{
	protected static void SetPlatformText(PlatformView platformView, string text) => ((TextBox)platformView).Text = text;

	protected static bool IsPlatformViewFocused(PlatformView platformView) => ((TextBox)platformView).FocusState is not FocusState.Unfocused;
}