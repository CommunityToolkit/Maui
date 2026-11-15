using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.Animations;

static partial class FlipProjection
{
	// .NET MAUI on Windows keeps the old PlaneProjection when RotationX and RotationY return to 0 while another transform is set
	static partial void RemoveStaleProjection(VisualElement view)
	{
		if (view.RotationX != 0 || view.RotationY != 0)
		{
			return;
		}

		if (view.Handler is IPlatformViewHandler handler && (handler.ContainerView ?? handler.PlatformView) is FrameworkElement platformView && platformView.Projection is not null)
		{
			platformView.Projection = null;
		}
	}
}