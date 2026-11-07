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

		// An app's LayoutHandler.PlatformViewFactory also creates the platform view of this handler, in place of CreatePlatformView, and the header
		// then has no keyboard or UI Automation support, as before the Expander had its own handler
		(platformView as ExpanderLayoutPanel)?.UpdateHeaderAccessibility();
	}
}
