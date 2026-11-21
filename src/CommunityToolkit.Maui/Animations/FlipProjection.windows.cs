using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace CommunityToolkit.Maui.Animations;

static partial class FlipProjection
{
	static partial void GetCurrentProjection(VisualElement view, ref object? projection) => projection = GetTransformedView(view)?.Projection;

	// .NET MAUI on Windows keeps the old PlaneProjection when RotationX and RotationY return to 0 while another transform is set.
	// MAUI writes a new PlaneProjection whenever the view turns and never another type, so the projection the flip started with, or another type, belongs to the app and stays.
	static partial void RemoveStaleProjection(VisualElement view, object? projectionAtStart)
	{
		if (view.RotationX != 0 || view.RotationY != 0)
		{
			return;
		}

		if (GetTransformedView(view) is FrameworkElement platformView && platformView.Projection is PlaneProjection projection && !ReferenceEquals(projection, projectionAtStart))
		{
			platformView.Projection = null;
		}
	}

	// .NET MAUI's mappers write the transforms to the container view when the handler has one (ToPlatform())
	static FrameworkElement? GetTransformedView(VisualElement view) =>
		view.Handler is IPlatformViewHandler handler ? (handler.ContainerView ?? handler.PlatformView) as FrameworkElement : null;
}