using Android.Animation;

namespace CommunityToolkit.Maui.DeviceTests;

// ValueAnimator.getDurationScale/setDurationScale are on the unsupported-app-usage list (setDurationScale is hidden on every API level,
// getDurationScale below API 26), so they are reached through Java reflection. Only this process's scale changes, never the global setting.
static class AnimatorDurationScale
{
	public static bool IsEnabled => OperatingSystem.IsAndroidVersionAtLeast(26) ? ValueAnimator.AreAnimatorsEnabled() : Get() > 0f;

	public static float Get()
	{
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

	/// <returns>The error when the scale could not be switched on; <paramref name="scaleToRestore"/> is the earlier scale when this call changed it.</returns>
	public static string? TryEnable(out float? scaleToRestore)
	{
		scaleToRestore = null;

		try
		{
			if (IsEnabled)
			{
				return null;
			}

			var previous = Get();
			var error = TrySet(1f);
			if (error is null)
			{
				scaleToRestore = previous;
			}

			return error;
		}
		catch (Java.Lang.Throwable ex)
		{
			return $"{ex.GetType().Name}: {ex.Message}";
		}
	}
}
