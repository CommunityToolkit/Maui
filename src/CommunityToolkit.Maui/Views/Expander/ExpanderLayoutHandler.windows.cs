using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Views;

sealed partial class ExpanderLayoutHandler
{
	// An app's LayoutHandler.PlatformViewFactory creates the platform view of every layout handler, this one included, and only an
	// ExpanderLayoutPanel gives the header its keyboard and UI Automation support
	public override void SetVirtualView(IView view) => this.SetVirtualViewWithItsOwnPlatformView(view, base.SetVirtualView, CreatePlatformView);

	protected override LayoutPanel CreatePlatformView() => new ExpanderLayoutPanel
	{
		CrossPlatformLayout = VirtualView
	};

	protected override void ConnectHandler(LayoutPanel platformView)
	{
		base.ConnectHandler(platformView);

		// SetVirtualView gets the ExpanderLayoutPanel even with an app's LayoutHandler.PlatformViewFactory only because MAUI creates the platform view
		// inside SetVirtualView, so if a later MAUI does not, the header loses its keyboard and UI Automation support instead of making the Expander throw
		(platformView as ExpanderLayoutPanel)?.UpdateHeaderAccessibility();
	}
}