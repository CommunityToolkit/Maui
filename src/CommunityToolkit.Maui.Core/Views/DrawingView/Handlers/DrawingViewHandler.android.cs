using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Views;

public partial class DrawingViewHandler : ViewHandler<IDrawingView, DrawingNativeView>
{
	/// <inheritdoc />
	protected override DrawingNativeView CreateNativeView()
	{
		return new DrawingNativeView(VirtualView, Context);
	}
}