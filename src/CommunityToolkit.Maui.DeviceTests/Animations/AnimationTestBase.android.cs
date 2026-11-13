using System.Globalization;
using Android.Animation;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class AnimationTestBase
{
	float? animatorScaleToRestore;

	protected static double GetNativeOpacity(IPlatformViewHandler handler) => GetTransformedView(handler).Alpha;

	// .NET MAUI's mappers write to the container view when the handler has one (ToPlatform())
	static AView GetTransformedView(IPlatformViewHandler handler) => (AView)(handler.ContainerView ?? handler.PlatformView!);

	static string DescribeNativeValues(IPlatformViewHandler handler) => DescribeNativeValues(GetTransformedView(handler));

	// The extensions that ViewHandler's mappers call, applied to a view that no window shows
	static string DescribeExpectedNativeValues(IPlatformViewHandler handler, VisualElement view)
	{
		using var expected = new AView(GetTransformedView(handler).Context);
		expected.UpdateOpacity(view);
		expected.UpdateTranslationX(view);
		expected.UpdateTranslationY(view);
		expected.UpdateScale(view);
		expected.UpdateRotation(view);
		expected.UpdateRotationX(view);
		expected.UpdateRotationY(view);
		expected.UpdateAnchorX(view);
		expected.UpdateAnchorY(view);
		return DescribeNativeValues(expected);
	}

	static string DescribeNativeValues(AView view) => string.Create(CultureInfo.InvariantCulture,
		$"alpha {FormatNativeValue(view.Alpha)}, scale ({FormatNativeValue(view.ScaleX)}, {FormatNativeValue(view.ScaleY)}), rotation ({FormatNativeValue(view.Rotation)}, {FormatNativeValue(view.RotationX)}, {FormatNativeValue(view.RotationY)}), translation px ({FormatNativeValue(view.TranslationX)}, {FormatNativeValue(view.TranslationY)}), pivot px ({FormatNativeValue(view.PivotX)}, {FormatNativeValue(view.PivotY)})");

	static string DescribeNativeLayout(IPlatformViewHandler handler)
	{
		var view = GetTransformedView(handler);
		var locationInWindow = new int[2];
		view.GetLocationInWindow(locationInWindow);
		return string.Create(CultureInfo.InvariantCulture, $"bounds px ({view.Left}, {view.Top}, {view.Width}, {view.Height}), in window px ({locationInWindow[0]}, {locationInWindow[1]})");
	}

	// Same approach as TouchBehaviorTestBase.EnableAnimationsAsync. The pool emulators (like CI's) run with the global animator duration scale
	// at 0, and MAUI then jumps to the end of every animation (it asks ValueAnimator.AreAnimatorsEnabled), so no animation could start while
	// another one runs. The scale is switched on for this process only, through ValueAnimator.setDurationScale (a hidden API on the
	// unsupported-app-usage list), and never through the global setting: nothing outlives the test process.
	protected async Task EnableAnimationsAsync(IPlatformViewHandler handler)
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(26), "ValueAnimator.DurationScale and AreAnimatorsEnabled need API 26: animations cannot be switched on in-process here.");

		string? error = null;
		await InvokeOnMainThreadAsync(() =>
		{
			if (!ValueAnimator.AreAnimatorsEnabled())
			{
				var previous = ValueAnimator.DurationScale;
				error = TrySetAnimatorDurationScale(1f);
				if (error is null)
				{
					animatorScaleToRestore ??= previous;
				}
			}

			// The ticker learns about the new scale through its duration scale listener (API 33+) or, before that, through its energy saver
			// callback, which re-reads it.
			(handler.MauiContext?.Services.GetService(typeof(ITicker)) as IEnergySaverListener)?.OnStatusUpdated(false);
		});

		Skip.If(error is not null, $"The system's animations are off and could not be switched on for this process ({error}), so MAUI jumps to the end of every animation.");

		var ticker = handler.MauiContext?.Services.GetService(typeof(ITicker)) as ITicker;
		Assert.True(ValueAnimator.AreAnimatorsEnabled(), "Animations must be enabled for this process.");
		Assert.True(ticker is null || ticker.SystemEnabled, "MAUI's ticker must see the enabled animations.");
	}

	ValueTask PlatformDisposeAsync()
	{
		if (animatorScaleToRestore is not float scale)
		{
			return ValueTask.CompletedTask;
		}

		animatorScaleToRestore = null;
		return new ValueTask(InvokeOnMainThreadAsync(() => TrySetAnimatorDurationScale(scale)));
	}

	static string? TrySetAnimatorDurationScale(float scale)
	{
		try
		{
			using var valueAnimatorClass = Java.Lang.Class.FromType(typeof(ValueAnimator));
			using var setDurationScale = valueAnimatorClass.GetDeclaredMethod("setDurationScale", Java.Lang.Float.Type!)
				?? throw new InvalidOperationException("ValueAnimator.setDurationScale was not found.");
			setDurationScale.Invoke(null, Java.Lang.Float.ValueOf(scale));
			return null;
		}
		catch (Java.Lang.Throwable ex)
		{
			return $"{ex.GetType().Name}: {ex.Message}";
		}
	}
}