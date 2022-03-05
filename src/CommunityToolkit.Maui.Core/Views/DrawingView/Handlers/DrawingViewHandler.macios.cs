using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Views;

public partial class DrawingViewHandler : ViewHandler<IDrawingView, DrawingNativeView>
{
	/// <inheritdoc />
	protected override DrawingNativeView CreateNativeView()
	{
		return new DrawingNativeView(VirtualView);
	}

	/// <inheritdoc />
	protected override void ConnectHandler(DrawingNativeView nativeView)
	{
		base.ConnectHandler(nativeView);
		nativeView.Initialize();
	}

	/// <inheritdoc />
	protected override void DisconnectHandler(DrawingNativeView nativeView)
	{
		base.DisconnectHandler(nativeView);
		nativeView.CleanUp();
	}
}