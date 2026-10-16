using Android.OS;
using Android.Views;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using AImageView = Android.Widget.ImageView;
using PlatformView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// The tests send hover through DispatchTouchEvent too: the behavior also handles the HoverEnter and HoverExit that reach its View.Touch
// listener.
public partial class ImageTouchBehaviorTests
{
	static bool CanAssertPlatformViewIsCollected => false;

	static Aspect? GetNativeAspect(PlatformView platformView)
	{
		var scaleType = ((AImageView)platformView).GetScaleType();

		if (scaleType is null)
		{
			return null;
		}

		if (scaleType.Equals(AImageView.ScaleType.FitCenter))
		{
			return Aspect.AspectFit;
		}

		if (scaleType.Equals(AImageView.ScaleType.CenterCrop))
		{
			return Aspect.AspectFill;
		}

		if (scaleType.Equals(AImageView.ScaleType.FitXy))
		{
			return Aspect.Fill;
		}

		return scaleType.Equals(AImageView.ScaleType.Center) ? Aspect.Center : null;
	}

	static string DescribeNativeAspect(PlatformView platformView) => $"{platformView.GetType().Name}: scale type {((AImageView)platformView).GetScaleType()}";

	static bool HasNativeImage(PlatformView platformView) => ((AImageView)platformView).Drawable is not null;

	static Task PressAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		Send(handler, MotionEventActions.Down);

		return Task.CompletedTask;
	}

	static Task ReleaseAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		Send(handler, MotionEventActions.Up);

		return Task.CompletedTask;
	}

	static Task HoverEnterAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		Send(handler, MotionEventActions.HoverEnter);

		return Task.CompletedTask;
	}

	static Task HoverExitAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		Send(handler, MotionEventActions.HoverExit);

		return Task.CompletedTask;
	}

	static Task TouchAfterDetachAsync(IPlatformViewHandler handler, TouchBehavior behavior)
	{
		Send(handler, MotionEventActions.Down);
		Send(handler, MotionEventActions.Up);
		Send(handler, MotionEventActions.HoverEnter);
		Send(handler, MotionEventActions.HoverExit);

		Assert.Equal(HoverState.Default, behavior.CurrentHoverState);

		return Task.CompletedTask;
	}

	static void Send(IPlatformViewHandler handler, MotionEventActions action)
	{
		var time = SystemClock.UptimeMillis();
		using var motionEvent = MotionEvent.Obtain(time, time, action, 5, 5, 0) ?? throw new InvalidOperationException("Android returned no MotionEvent.");

		handler.PlatformView!.DispatchTouchEvent(motionEvent);
	}
}