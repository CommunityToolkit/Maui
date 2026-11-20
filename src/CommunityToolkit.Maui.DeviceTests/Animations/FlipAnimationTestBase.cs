using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

// A flip from 0 is the case that .NET MAUI on Windows handles apart from every other value, so it needs a view whose other transforms are set
public abstract partial class FlipAnimationTestBase<TAnimation> : ToAnimationTestBase<TAnimation> where TAnimation : RotateAnimation
{
	const double originalRotation = 10;
	const double originalTranslationX = 5;

	IReadOnlyList<(BindableProperty Property, double Value)> OriginalValuesFromZeroWithOtherTransformsSet =>
		[(PeakProperty, 0), (VisualElement.RotationProperty, originalRotation), (VisualElement.TranslationXProperty, originalTranslationX)];

	[Fact]
	public async Task FlipFromZeroWithOtherTransformsSetReturnsThePlatformViewToRest()
	{
		SetupBuilder();

		var originalValues = OriginalValuesFromZeroWithOtherTransformsSet;
		var view = CreateAnimatedView(originalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var run = await StartAndActMidRunAsync(view, () => CreateAnimation().Animate(view),
				() => AssertPlatformViewFollows(handler, view, nativeStateAtRest, "in the middle of the flip"),
				async missedRun =>
				{
					await missedRun.WaitAsync(waitTimeout);
					await AssertStaysAtOriginalValuesAsync(handler, view, originalValues, nativeStateAtRest, "after a flip that ended before the test could act, from 0 with other transforms set");
				});

			await run.WaitAsync(waitTimeout);
			await AssertStaysAtOriginalValuesAsync(handler, view, originalValues, nativeStateAtRest, "after a flip from 0 with other transforms set");
		});
	}
}
