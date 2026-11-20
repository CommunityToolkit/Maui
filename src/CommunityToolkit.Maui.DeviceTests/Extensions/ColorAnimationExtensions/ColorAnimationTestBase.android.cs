namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class ColorAnimationTestBase
{
	float? animatorScaleToRestore;

	// Below API 26 MAUI's ticker only checks battery saver, so with the animator scale at 0 its ValueAnimator stops after one frame and
	// the animation never ends. The scale is switched on for this process only; the global setting is never touched.
	protected async Task EnsureAnimationsCanEndAsync()
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			return;
		}

		string? error = null;
		await InvokeOnMainThreadAsync(() =>
		{
			error = AnimatorDurationScale.TryEnable(out var previous);
			if (previous is float scale)
			{
				animatorScaleToRestore ??= scale;
			}
		});

		Assert.True(error is null, $"The animator duration scale could not be switched on for this process ({error}).");
		Assert.True(AnimatorDurationScale.IsEnabled, "The animator duration scale must be on for this process.");
	}

	ValueTask PlatformDisposeAsync()
	{
		if (animatorScaleToRestore is not float scale)
		{
			return ValueTask.CompletedTask;
		}

		animatorScaleToRestore = null;
		return new ValueTask(InvokeOnMainThreadAsync(() => AnimatorDurationScale.TrySet(scale)));
	}
}
