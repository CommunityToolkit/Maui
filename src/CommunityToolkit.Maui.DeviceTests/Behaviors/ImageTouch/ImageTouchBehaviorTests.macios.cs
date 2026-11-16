using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Foundation;
using UIKit;
using PlatformView = UIKit.UIView;

namespace CommunityToolkit.Maui.DeviceTests;

// UIKit has no public way to create a UITouch, so the tests call TouchesBegan and TouchesEnded of the behavior's recognizer with an empty
// touch set. The hover recognizer (UIHoverGestureRecognizer) cannot be driven from code, so hovering calls the behavior's internal
// HandleHover, which the recognizer calls too.
public partial class ImageTouchBehaviorTests
{
	static bool CanAssertPlatformViewIsCollected => true;

	const string touchRecognizerName = "TouchUITapGestureRecognizer";

	static Aspect? GetNativeAspect(PlatformView platformView)
	{
		var contentMode = platformView switch
		{
			UIImageView imageView => imageView.ContentMode,
			UIButton button => button.ImageView.ContentMode,
			_ => throw new InvalidOperationException($"{platformView.GetType()} has no image.")
		};

		return contentMode switch
		{
			UIViewContentMode.ScaleAspectFit => Aspect.AspectFit,
			UIViewContentMode.ScaleAspectFill => Aspect.AspectFill,
			UIViewContentMode.ScaleToFill => Aspect.Fill,
			UIViewContentMode.Center => Aspect.Center,
			_ => null
		};
	}

	static string DescribeNativeAspect(PlatformView platformView) => platformView switch
	{
		UIImageView imageView => $"{imageView.GetType().Name}: content mode {imageView.ContentMode}",
		UIButton button => $"{button.GetType().Name}: content mode {button.ImageView.ContentMode}",
		_ => platformView.GetType().Name
	};

	static bool HasNativeImage(PlatformView platformView) => platformView switch
	{
		UIImageView imageView => imageView.Image is not null,
		UIButton button => button.CurrentImage is not null,
		_ => throw new InvalidOperationException($"{platformView.GetType()} has no image.")
	};

	static UIGestureRecognizer? FindTouchRecognizer(IPlatformViewHandler handler) =>
		handler.PlatformView!.GestureRecognizers?.SingleOrDefault(static recognizer => recognizer.GetType().Name == touchRecognizerName);

	static UIGestureRecognizer GetTouchRecognizer(IPlatformViewHandler handler) =>
		FindTouchRecognizer(handler) ?? throw new InvalidOperationException($"The platform view has no {touchRecognizerName}.");

	static Task PressAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		GetTouchRecognizer(handler).TouchesBegan(new NSSet(), new UIEvent());

		return Task.CompletedTask;
	}

	static Task ReleaseAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		GetTouchRecognizer(handler).TouchesEnded(new NSSet(), new UIEvent());

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

	// The behavior removed its recognizers when it detached: there is nothing left to send a touch to.
	static Task TouchAfterDetachAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		Assert.Null(FindTouchRecognizer(handler));
		Assert.DoesNotContain(handler.PlatformView!.GestureRecognizers ?? [], static recognizer => recognizer is UIHoverGestureRecognizer);

		return Task.CompletedTask;
	}
}