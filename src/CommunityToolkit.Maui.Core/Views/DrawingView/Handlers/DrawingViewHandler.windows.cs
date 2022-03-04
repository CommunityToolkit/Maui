using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView handler
/// </summary>

public partial class DrawingViewHandler : ViewHandler<IDrawingView, DrawingNativeView>
{
	/// <inheritdoc />
	protected override DrawingNativeView CreateNativeView() => new (VirtualView);
}