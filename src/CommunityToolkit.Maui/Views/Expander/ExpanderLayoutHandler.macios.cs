using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Views;

sealed partial class ExpanderLayoutHandler
{
	protected override LayoutView CreatePlatformView() => new ExpanderLayoutView
	{
		CrossPlatformLayout = VirtualView
	};
}
