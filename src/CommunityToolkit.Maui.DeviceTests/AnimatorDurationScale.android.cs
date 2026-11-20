using System.Diagnostics.CodeAnalysis;
using Android.Animation;

namespace CommunityToolkit.Maui.DeviceTests;

// setDurationScale is hidden on every API level and getDurationScale below API 26, so they are reached through Java reflection (from
// API 26 the public binding reads the scale). Only this process's scale changes, never the global setting.
static class AnimatorDurationScale
{
	public static bool IsEnabled => OperatingSystem.IsAndroidVersionAtLeast(26) ? ValueAnimator.AreAnimatorsEnabled() : Get() > 0f;

	public static float Get()
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			return ValueAnimator.DurationScale;
		}

		using var valueAnimatorClass = Java.Lang.Class.FromType(typeof(ValueAnimator));
		using var getDurationScale = valueAnimatorClass.GetDeclaredMethod("getDurationScale")
			?? throw new InvalidOperationException("ValueAnimator.getDurationScale was not found.");
		return ((Java.Lang.Float)getDurationScale.Invoke(null)!).FloatValue();
	}

	public static string? TrySet(float scale)
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

	public static bool TryEnable(out float? scaleToRestore, [NotNullWhen(false)] out string? error)
	{
		scaleToRestore = null;
		error = null;

		try
		{
			if (IsEnabled)
			{
				return true;
			}

			var previous = Get();
			error = TrySet(1f);
			if (error is null)
			{
				scaleToRestore = previous;
			}

			return error is null;
		}
		catch (Java.Lang.Throwable ex)
		{
			error = $"{ex.GetType().Name}: {ex.Message}";
			return false;
		}
	}
}
