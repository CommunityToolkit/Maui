using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

// A flip from 0 is the case that .NET MAUI on Windows handles apart from every other value, so it needs a view whose other transforms are set
public abstract partial class FlipAnimationTestBase<TAnimation> : ToAnimationTestBase<TAnimation> where TAnimation : RotateAnimation
{
	const double originalRotation = 10;
	const double originalTranslationX = 5;

	[Fact]
	public async Task FlipFromZeroWithOtherTransformsSetReturnsThePlatformViewToRest()
	{
		SetupBuilder();

		IReadOnlyList<(BindableProperty Property, double Value)> originalValues =
			[(PeakProperty, 0), (VisualElement.RotationProperty, originalRotation), (VisualElement.TranslationXProperty, originalTranslationX)];
		var view = CreateAnimatedView(originalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var midRun = WaitForMidRunAsync(view);
			var run = CreateAnimation().Animate(view);
			await midRun;

			AssertPlatformViewFollows(handler, view, nativeStateAtRest, "in the middle of the flip");

			await run.WaitAsync(waitTimeout);
			await AssertStaysAtOriginalValuesAsync(handler, view, originalValues, nativeStateAtRest, "after a flip from 0 with other transforms set");
		});
	}
}
