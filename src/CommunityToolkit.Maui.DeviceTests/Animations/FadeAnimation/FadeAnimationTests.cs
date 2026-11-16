using System.ComponentModel;
using System.Threading.Channels;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.DeviceTests;

// An AnimationBehavior triggered again during its animation (a second tap) cancels the running animation and starts a new one, and the
// cancelled animation continues on the UI thread from inside that cancellation. So the tests need the platform's own animation ticker and
// UI thread.
[Category(TestCategory.FadeAnimation)]
[Collection(TestCollections.DisableParallelization)]
public partial class FadeAnimationTests : CommunityToolkitHandlerTestBase
{
	const double originalOpacity = 0.9;
	const double fadedOpacity = 0.2;
	const double opacityTolerance = 0.001;

	// Each fade (out, then back) lasts long enough to start the next animation in the middle of it, even on a slow emulator.
	const uint fadeLength = 800;

	static readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(10);

	// An animation started mid-fade must not take the partially faded opacity as the one to return to: the view would stay dim, also after
	// every later animation.
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

	// Continuations run asynchronously: the test continues after the animation frame that met the last condition, as it would after a tap.
	static Task WaitForOpacityAsync(VisualElement view, params Func<double, bool>[] conditionsInOrder)
	{
		var conditionsMet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var nextCondition = 0;

		view.PropertyChanged += HandlePropertyChanged;

		return conditionsMet.Task.WaitAsync(waitTimeout);

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(VisualElement.Opacity) && conditionsInOrder[nextCondition](view.Opacity) && ++nextCondition == conditionsInOrder.Length)
			{
				view.PropertyChanged -= HandlePropertyChanged;
				conditionsMet.SetResult();
			}
		}
	}

	// AnimationBehavior starts animations without handing out their Task, so the animation reports each end itself, however it ended.
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