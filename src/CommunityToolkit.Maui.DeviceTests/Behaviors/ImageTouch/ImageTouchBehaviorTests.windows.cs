using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using WStretch = Microsoft.UI.Xaml.Media.Stretch;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;

namespace CommunityToolkit.Maui.DeviceTests;

// WinUI cannot raise pointer events from code, so touch and hover call the behavior's internal HandleTouch and HandleHover,
// which its pointer handlers call too.
public partial class ImageTouchBehaviorTests
{
	// Not verified on a device: the WinUI element is only reachable through COM wrappers, so the toolkit-owned managed objects are what the leak tests require.
	static bool CanAssertPlatformViewIsCollected => false;

	static WImage GetNativeImage(PlatformView platformView) => platformView switch
	{
		WImage image => image,
		WButton { Content: WImage buttonImage } => buttonImage,
		WButton { Content: Microsoft.UI.Xaml.Controls.Panel panel } when panel.Children.OfType<WImage>().FirstOrDefault() is WImage panelImage => panelImage,
		_ => throw new InvalidOperationException($"{platformView.GetType()} has no image.")
	};

	static Aspect? GetNativeAspect(PlatformView platformView) => GetNativeImage(platformView).Stretch switch
	{
		WStretch.Uniform => Aspect.AspectFit,
		WStretch.UniformToFill => Aspect.AspectFill,
		WStretch.Fill => Aspect.Fill,
		WStretch.None => Aspect.Center,
		_ => null
	};

	static string DescribeNativeAspect(PlatformView platformView) => $"{platformView.GetType().Name}: stretch {GetNativeImage(platformView).Stretch}";

	static bool HasNativeImage(PlatformView platformView) => GetNativeImage(platformView).Source is not null;

	static Task PressAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		behavior.HandleUserInteraction(TouchInteractionStatus.Started);
		behavior.HandleTouch(TouchStatus.Started);

		return Task.CompletedTask;
	}

	static Task ReleaseAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		behavior.HandleTouch(TouchStatus.Completed);
		behavior.HandleUserInteraction(TouchInteractionStatus.Completed);

		return Task.CompletedTask;
	}

	static Task HoverEnterAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		behavior.HandleHover(HoverStatus.Entered);

		return Task.CompletedTask;
	}

	static Task HoverExitAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		behavior.HandleHover(HoverStatus.Exited);

		return Task.CompletedTask;
	}

	// The behavior ignores touch while it has no element, as it does after it detached.
	static Task TouchAfterDetachAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		behavior.HandleTouch(TouchStatus.Started);

		return Task.CompletedTask;
	}
}