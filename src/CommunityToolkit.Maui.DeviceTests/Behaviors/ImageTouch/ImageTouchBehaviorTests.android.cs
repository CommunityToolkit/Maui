using Android.OS;
using Android.Views;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using AImageView = Android.Widget.ImageView;
using PlatformView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// Android: touch and hover are sent to the platform view as MotionEvents, which reach the View.Touch listener of the behavior
// (the behavior also treats HoverEnter and HoverExit that arrive there). The aspect is read from the ImageView's scale type.
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

	// The behavior removed its touch listener when it detached: nothing may react to these events.
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