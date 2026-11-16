using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Views;

sealed partial class ExpanderLayoutHandler
{
	// An app's LayoutHandler.PlatformViewFactory creates the platform view of every layout handler, this one included, and only an
	// ExpanderLayoutView makes VoiceOver read the header as one button
	public override void SetVirtualView(IView view) => this.SetVirtualViewWithItsOwnPlatformView(view, base.SetVirtualView, CreatePlatformView);

	protected override LayoutView CreatePlatformView() => new ExpanderLayoutView
	{
		CrossPlatformLayout = VirtualView
	};
}