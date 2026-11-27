using System.Globalization;
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
		using var expectedView = new AView(GetTransformedView(handler).Context);
		expectedView.UpdateOpacity(view);
		expectedView.UpdateTranslationX(view);
		expectedView.UpdateTranslationY(view);
		expectedView.UpdateScale(view);
		expectedView.UpdateRotation(view);
		expectedView.UpdateRotationX(view);
		expectedView.UpdateRotationY(view);
		expectedView.UpdateAnchorX(view);
		expectedView.UpdateAnchorY(view);
		return DescribeNativeValues(expectedView);
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

	// Same approach as TouchBehaviorTestBase.EnableAnimationsAsync. CI's emulators run with the animator scale at 0: from API 26 MAUI then jumps to
	// the end of every animation, so no animation could start while another one runs, and below 26 its ticker stalls.
	// The scale is switched on for this process only, so a crash cannot leave a shared emulator with animations on.
	protected async Task EnableAnimationsAsync(IPlatformViewHandler handler)
	{
		string? error = null;
		await InvokeOnMainThreadAsync(() =>
		{
			error = AnimatorDurationScale.TryEnable(out var previous);
			if (previous is float scale)
			{
				animatorScaleToRestore ??= scale;
			}

			// Before API 33 MAUI's ticker re-reads the animator state only in its energy saver callback.
			(handler.MauiContext?.Services.GetService(typeof(ITicker)) as IEnergySaverListener)?.OnStatusUpdated(false);
		});

		Assert.True(error is null, $"The animator duration scale could not be switched on for this process ({error}).");

		var ticker = handler.MauiContext?.Services.GetService(typeof(ITicker)) as ITicker;
		Assert.True(AnimatorDurationScale.IsEnabled, "Animations must be enabled for this process.");
		Assert.True(ticker is null || ticker.SystemEnabled, "MAUI's ticker must see the enabled animations.");
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