using System.ComponentModel;
using System.Threading.Channels;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of FadeAnimation (Animations/FadeAnimation.shared.cs) started while it runs, as AnimationBehavior does when it is triggered
// again during its animation (a second tap): it cancels the running animation and starts a new one, and the cancelled animation continues on
// the UI thread from inside that cancellation. The tests run on the platform's animation ticker and UI thread, with the view in the
// runner's window, and read the platform view's opacity through the platform partials (FadeAnimationTests.android/macios/windows.cs).
[Category(TestCategory.FadeAnimation)]
[Collection(TestCollections.DisableParallelization)]
public partial class FadeAnimationTests : CommunityToolkitHandlerTestBase
{
	const double originalOpacity = 0.9;
	const double fadedOpacity = 0.2;
	const double opacityTolerance = 0.001;

	// Each of the two fades of an animation (out, then back) lasts this long: long enough to start the next animation in the middle of either
	// fade on a slow emulator.
	const uint fadeLength = 800;

	static readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(10);

	// Catches: an animation started while the previous one fades the view out returning the view to the partially faded opacity it started
	// from instead of its original opacity, so the view stays dim, also after every later animation.
	[Fact]
	public async Task AnimationBehaviorTriggeredWhileFadingOutReturnsToOriginalOpacity()
	{
		SetupBuilder();

		var animation = new ObservedFadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
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

	// Catches: the same while the previous animation fades the view back.
	[Fact]
	public async Task AnimationBehaviorTriggeredWhileFadingBackReturnsToOriginalOpacity()
	{
		SetupBuilder();

		var animation = new ObservedFadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
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

	// Catches: a FadeAnimation started on a view that another FadeAnimation is fading (without AnimationBehavior, nothing cancels the running
	// animation first) returning the view to the partially faded opacity, or the replaced animation fading the view back over the new one.
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

	// Runs after every test: the base class disposes the test's MauiApp first, then the platform partial undoes what the test changed
	// (Android: the in-process animator duration scale).
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			await PlatformDisposeAsync();
		}
	}

	// Triggers the behavior while its animation runs, then once more after both animations ended
	static async Task TriggerAgainAndAssertOriginalOpacityAsync(IPlatformViewHandler handler, VisualElement view, AnimationBehavior behavior, ObservedFadeAnimation animation)
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

	// Completes once the opacity of the view, as it changes, has met each condition in turn. The test continues after the animation frame
	// that met the last condition, as it would after a tap.
	static Task WaitForOpacityAsync(VisualElement view, params Func<double, bool>[] conditions)
	{
		var conditionsMet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var nextCondition = 0;

		view.PropertyChanged += HandlePropertyChanged;

		return conditionsMet.Task.WaitAsync(waitTimeout);

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(VisualElement.Opacity) && conditions[nextCondition](view.Opacity) && ++nextCondition == conditions.Length)
			{
				view.PropertyChanged -= HandlePropertyChanged;
				conditionsMet.SetResult();
			}
		}
	}

	// Counts its animations that ended, however they ended: AnimationBehavior starts them without handing out their Task
	sealed class ObservedFadeAnimation : FadeAnimation
	{
		readonly Channel<bool> endedAnimations = Channel.CreateUnbounded<bool>();

		public async Task WaitForEndedAnimationsAsync(int count)
		{
			for (var i = 0; i < count; i++)
			{
				await endedAnimations.Reader.ReadAsync().AsTask().WaitAsync(waitTimeout);
			}
		}

		public override async Task Animate(VisualElement view, CancellationToken token = default)
		{
			try
			{
				await base.Animate(view, token);
			}
			finally
			{
				endedAnimations.Writer.TryWrite(true);
			}
		}
	}
}