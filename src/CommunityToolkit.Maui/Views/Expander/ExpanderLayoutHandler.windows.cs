using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Views;

sealed partial class ExpanderLayoutHandler
{
	protected override LayoutPanel CreatePlatformView() => new ExpanderLayoutPanel
	{
		CrossPlatformLayout = VirtualView
	};

	protected override void ConnectHandler(LayoutPanel platformView)
	{
		base.ConnectHandler(platformView);
		((ExpanderLayoutPanel)platformView).UpdateHeaderAccessibility();
	}
}
