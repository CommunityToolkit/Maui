using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.DeviceTests;

// An AnimationBehavior triggered again during its animation (a second tap) cancels the running animation and starts a new one, and the
// cancelled animation continues on the UI thread from inside that cancellation. So the tests need the platform's own animation ticker and
// UI thread.
[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class FadeAnimationTests : ToAnimationTestBase<FadeAnimation>
{
	const double originalOpacity = 0.9;
	const double fadedOpacity = 0.2;
	const double opacityTolerance = 0.001;

	// Length covers fading out and back (discussions/1231): 800 ms each way leaves time to start the next animation in the middle of either fade, even on a slow emulator
	const uint fadeLength = 1600;

	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.OpacityProperty, originalOpacity)];

	protected override BindableProperty PeakProperty => VisualElement.OpacityProperty;

	protected override double PeakValue => fadedOpacity;

	protected override FadeAnimation CreateAnimation() => new() { Opacity = fadedOpacity, Length = fadeLength };

	// An animation started mid-fade must not take the partially faded opacity as the one to return to: the view would stay dim, also after
	// every later animation.
	[Fact]
	public async Task AnimationBehaviorTriggeredWhileFadingOutReturnsToOriginalOpacity()
	{
		SetupBuilder();

		var animation = new ObservedAnimation(new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength });
		var behavior = new AnimationBehavior { AnimationType = animation };
		var border = CreateBorder(behavior);

		await AttachAndRun(border, async handler =>
		{
			await border.OnLoadedAsync();
			await EnableAnimationsAsync(handler);

			var fadingOut = WaitForOpacityAsync(border, opacity => opacity < originalOpacity - 0.2);
			behavior.AnimateCommand.Execute(CancellationToken.None);
			await fadingOut;

			await TriggerAgainAndAssertOriginalOpacityAsync(handler, border, behavior, animation);
		});
	}

	[Fact]
	public async Task AnimationBehaviorTriggeredWhileFadingBackReturnsToOriginalOpacity()
	{
		SetupBuilder();

		var animation = new ObservedAnimation(new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength });
		var behavior = new AnimationBehavior { AnimationType = animation };
		var border = CreateBorder(behavior);

		await AttachAndRun(border, async handler =>
		{
			await border.OnLoadedAsync();
			await EnableAnimationsAsync(handler);

			var fadingBack = WaitForOpacityAsync(border, opacity => opacity <= fadedOpacity + opacityTolerance, opacity => opacity >= fadedOpacity + 0.3);
			behavior.AnimateCommand.Execute(CancellationToken.None);
			await fadingBack;

			await TriggerAgainAndAssertOriginalOpacityAsync(handler, border, behavior, animation);
		});
	}

	// Without AnimationBehavior nothing cancels the running animation first; the replaced animation must also not fade the view back over
	// the new one.
	[Fact]
	public async Task FadeAnimationStartedWhileAnotherFadesTheViewReturnsToOriginalOpacity()
	{
		SetupBuilder();

		var firstAnimation = new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
		var secondAnimation = new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
		var border = CreateBorder();

		await AttachAndRun(border, async handler =>
		{
			await border.OnLoadedAsync();
			await EnableAnimationsAsync(handler);

			var fadingOut = WaitForOpacityAsync(border, opacity => opacity < originalOpacity - 0.2);
			var firstFade = firstAnimation.Animate(border);
			await fadingOut;

			var fadedAgain = WaitForOpacityAsync(border, opacity => opacity <= fadedOpacity + opacityTolerance);
			var secondFade = secondAnimation.Animate(border);
			await Task.WhenAll(firstFade, secondFade).WaitAsync(waitTimeout);

			Assert.True(fadedAgain.IsCompletedSuccessfully, "The second animation did not fade the view out.");
			AssertOpacity(handler, border, originalOpacity, "after both animations ended");
		});
	}

	static async Task TriggerAgainAndAssertOriginalOpacityAsync(IPlatformViewHandler handler, VisualElement view, AnimationBehavior behavior, ObservedAnimation animation)
	{
		var fadedAgain = WaitForOpacityAsync(view, opacity => opacity <= fadedOpacity + opacityTolerance);
		behavior.AnimateCommand.Execute(CancellationToken.None);
		await animation.WaitForEndedAnimationsAsync(2);

		Assert.True(fadedAgain.IsCompletedSuccessfully, "The animation triggered again did not fade the view out.");
		AssertOpacity(handler, view, originalOpacity, "after the animation triggered again");

		behavior.AnimateCommand.Execute(CancellationToken.None);
		await animation.WaitForEndedAnimationsAsync(1);

		AssertOpacity(handler, view, originalOpacity, "after a later animation");
	}

	static Border CreateBorder(AnimationBehavior? behavior = null)
	{
		var border = new Border
		{
			WidthRequest = 100,
			HeightRequest = 100,
			BackgroundColor = Colors.Green,
			Opacity = originalOpacity
		};

		if (behavior is not null)
		{
			border.Behaviors.Add(behavior);
		}

		return border;
	}

	static void AssertOpacity(IPlatformViewHandler handler, VisualElement view, double expected, string when)
	{
		Assert.True(Math.Abs(view.Opacity - expected) <= opacityTolerance, $"The view's Opacity is {view.Opacity} {when}, expected {expected}.");

		var nativeOpacity = GetNativeOpacity(handler);
		Assert.True(Math.Abs(nativeOpacity - expected) <= opacityTolerance, $"The platform view's opacity is {nativeOpacity} {when}, expected {expected}.");
	}

	static Task WaitForOpacityAsync(VisualElement view, params Func<double, bool>[] conditionsInOrder) => WaitForValueAsync(view, VisualElement.OpacityProperty, conditionsInOrder);
}