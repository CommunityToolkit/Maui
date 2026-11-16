using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace CommunityToolkit.Maui.Animations;

static partial class FlipProjection
{
	// .NET MAUI on Windows keeps the old PlaneProjection when RotationX and RotationY return to 0 while another transform is set.
	// MAUI never writes any other projection type, so another one belongs to the app and stays.
	static partial void RemoveStaleProjection(VisualElement view)
	{
		if (view.RotationX != 0 || view.RotationY != 0)
		{
			return;
		}

		if (view.Handler is IPlatformViewHandler handler && (handler.ContainerView ?? handler.PlatformView) is FrameworkElement platformView && platformView.Projection is PlaneProjection)
		{
			platformView.Projection = null;
		}
	}
}