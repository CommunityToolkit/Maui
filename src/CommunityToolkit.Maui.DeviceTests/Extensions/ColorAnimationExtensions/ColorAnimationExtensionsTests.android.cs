using Android.Animation;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ColorAnimationExtensionsTests
{
	float? animatorScaleToRestore;

	// Below API 26 MAUI's ticker only checks battery saver, so with the animator scale at 0 its ValueAnimator stops after one frame and
	// the animation never ends. The scale is switched on for this process only; the global setting is never touched.
	async Task EnsureAnimationsCanEndAsync()
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			return;
		}

		float scale = 1f;
		await InvokeOnMainThreadAsync(() =>
		{
			using var valueAnimatorClass = Java.Lang.Class.FromType(typeof(ValueAnimator));
			using var getDurationScale = valueAnimatorClass.GetDeclaredMethod("getDurationScale")
				?? throw new InvalidOperationException("ValueAnimator.getDurationScale was not found.");
			using var setDurationScale = valueAnimatorClass.GetDeclaredMethod("setDurationScale", Java.Lang.Float.Type!)
				?? throw new InvalidOperationException("ValueAnimator.setDurationScale was not found.");

			var previous = ((Java.Lang.Float)getDurationScale.Invoke(null)!).FloatValue();
			if (previous <= 0f)
			{
				setDurationScale.Invoke(null, Java.Lang.Float.ValueOf(1f));
				animatorScaleToRestore ??= previous;
			}

			scale = ((Java.Lang.Float)getDurationScale.Invoke(null)!).FloatValue();
		});

		Assert.True(scale > 0f, "The animator duration scale must be on for this process.");
	}

	ValueTask PlatformDisposeAsync()
	{
		if (animatorScaleToRestore is not float scale)
		{
			return ValueTask.CompletedTask;
		}

		animatorScaleToRestore = null;
		return new ValueTask(InvokeOnMainThreadAsync(() =>
		{
			using var valueAnimatorClass = Java.Lang.Class.FromType(typeof(ValueAnimator));
			using var setDurationScale = valueAnimatorClass.GetDeclaredMethod("setDurationScale", Java.Lang.Float.Type!);
			setDurationScale?.Invoke(null, Java.Lang.Float.ValueOf(scale));
		}));
	}
}
