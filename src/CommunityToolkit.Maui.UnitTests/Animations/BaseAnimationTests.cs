using System.ComponentModel;
using System.Threading.Channels;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public abstract class BaseAnimationTests<TAnimation> : BaseTest where TAnimation : BaseAnimation, new()
{
	protected const double precision = 0.001;

	// Only a guard against a hang: a test that waits this long has failed anyway
	protected static readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(15);

	// Facts that check behaviour, not duration, run short so they fit the test timeouts on slow CI machines
	protected const uint functionalTestLength = 200;

	// Each Commit ends on the first 16 ms mock tick at or after its length; FadeAnimation, ScaleAnimation and the flips commit twice
	const uint maximumTickRoundingMilliseconds = 2 * 16;

	const uint customTimingTestLength = 500;

	protected static readonly (string Name, BindableProperty Property)[] animatableProperties =
	[
		(nameof(VisualElement.Opacity), VisualElement.OpacityProperty),
		(nameof(VisualElement.Scale), VisualElement.ScaleProperty),
		(nameof(VisualElement.ScaleX), VisualElement.ScaleXProperty),
		(nameof(VisualElement.ScaleY), VisualElement.ScaleYProperty),
		(nameof(VisualElement.Rotation), VisualElement.RotationProperty),
		(nameof(VisualElement.RotationX), VisualElement.RotationXProperty),
		(nameof(VisualElement.RotationY), VisualElement.RotationYProperty),
		(nameof(VisualElement.TranslationX), VisualElement.TranslationXProperty),
		(nameof(VisualElement.TranslationY), VisualElement.TranslationYProperty),
		(nameof(VisualElement.AnchorX), VisualElement.AnchorXProperty),
		(nameof(VisualElement.AnchorY), VisualElement.AnchorYProperty)
	];

	protected virtual TAnimation CreateAnimation() => new();

	protected TAnimation CreateFunctionalAnimation()
	{
		var animation = CreateAnimation();
		animation.Length = Math.Min(animation.Length, functionalTestLength);

		return animation;
	}

	// Where an interrupted animation leaves the view differs between the animation families
	protected virtual bool InterruptionLeavesViewMidAnimation => true;

	protected virtual void Interrupt(VisualElement view) => view.CancelAnimations();

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task LengthShouldDictateFullAnimationLength() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateAnimation();

		var elapsedMilliseconds = await MeasureElapsedAnimationMillisecondsAsync(animation, token);

		elapsedMilliseconds.Should().BeInRange(animation.Length, animation.Length + maximumTickRoundingMilliseconds);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task LengthShouldDictateFullAnimationLength_WithCustomLength() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateAnimation();
		animation.Length = customTimingTestLength;

		var elapsedMilliseconds = await MeasureElapsedAnimationMillisecondsAsync(animation, token);

		elapsedMilliseconds.Should().BeInRange(customTimingTestLength, customTimingTestLength + maximumTickRoundingMilliseconds);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Short)]
	public void Animate_NullView_ThrowsBeforeReturningTheTask()
	{
		var animation = CreateAnimation();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		NullArgumentAssert.Throws("view", () => { _ = animation.Animate(null, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task Animate_AlreadyCancelledToken_ThrowsOperationCanceledException()
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		var animationTask = animation.Animate(view, cancellationTokenSource.Token);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => animationTask);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ReturnsEveryPropertyToItsNonDefaultStartingValue() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_StartedWhileTheSameAnimationRuns_ReturnsEveryPropertyToItsValueBeforeTheFirstAnimation() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var firstAnimation = animation.Animate(view, token);
		await firstChange;

		var secondAnimation = animation.Animate(view, token);
		await Task.WhenAll(firstAnimation, secondAnimation).WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_TokenCancelledMidAnimation_ThrowsAndReturnsEveryPropertyToItsStartingValue() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var runningAnimation = animation.Animate(view, cancellationTokenSource.Token);
		await firstChange;

		// Cancel, not CancelAsync: the abort must run on this thread, which also ticks the animations, because MAUI's animation bookkeeping is not thread-safe
		cancellationTokenSource.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runningAnimation.WaitAsync(waitTimeout, token));

		AssertValuesEqual(view, startingValues);

		var changedAfterCancellation = false;
		view.PropertyChanged += (_, _) => changedAfterCancellation = true;
		// Proving that nothing happens needs a fixed quiet window
		await Task.Delay(TimeSpan.FromMilliseconds(150), token);

		changedAfterCancellation.Should().BeFalse("a cancelled animation stops animating");
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_StartedAfterAnInterruption_ReturnsEveryPropertyToItsValueBeforeTheFirstAnimation() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var firstAnimation = animation.Animate(view, token);
		await firstChange;

		Interrupt(view);
		await firstAnimation.WaitAsync(waitTimeout, token);

		HasAnyValueChanged(CaptureValues(view), startingValues).Should().Be(InterruptionLeavesViewMidAnimation);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_OnTwoViewsAtOnce_ReturnsEachViewToItsOwnStartingValues() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		var otherView = CreateOtherAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var otherStartingValues = CaptureValues(otherView);

		await Task.WhenAll(animation.Animate(view, token), animation.Animate(otherView, token)).WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, startingValues);
		AssertValuesEqual(otherView, otherStartingValues);
	}, TestContext.Current.CancellationToken);

	// Running the test on one thread, like an app runs its animations on its UI thread, makes the mock ticker of EnableAnimations fire on that
	// thread too. On the thread pool it races the test, and the tickers of other views, for MAUI's animation bookkeeping (a static Dictionary
	// that is not thread-safe).
	protected static Task RunOnSingleThreadAsync(Func<CancellationToken, Task> test, CancellationToken token) =>
		Task.Run(() => AsyncContext.Run(() => test(token)), token);

	protected static Label CreateAnimatedViewWithNonDefaultValues()
	{
		var label = new Label
		{
			Opacity = 0.9,
			Scale = 0.8,
			ScaleX = 1.1,
			ScaleY = 0.9,
			Rotation = 30,
			RotationX = 20,
			RotationY = 10,
			TranslationX = 12,
			TranslationY = 7,
			AnchorX = 0.3,
			AnchorY = 0.7
		};
		label.EnableAnimations();

		return label;
	}

	protected static Label CreateOtherAnimatedViewWithNonDefaultValues()
	{
		var label = new Label
		{
			Opacity = 0.6,
			Scale = 1.4,
			ScaleX = 0.7,
			ScaleY = 1.3,
			Rotation = -45,
			RotationX = 60,
			RotationY = -25,
			TranslationX = -9,
			TranslationY = 31,
			AnchorX = 0.6,
			AnchorY = 0.2
		};
		label.EnableAnimations();

		return label;
	}

	protected static async Task<long> MeasureElapsedAnimationMillisecondsAsync(BaseAnimation animation, CancellationToken token)
	{
		Label label = new();
		label.EnableAnimations();

		var startMilliseconds = label.GetElapsedAnimationMilliseconds();
		await animation.Animate(label, token).WaitAsync(waitTimeout, token);

		return label.GetElapsedAnimationMilliseconds() - startMilliseconds;
	}

	protected static Task WaitForPropertyAsync(VisualElement view, BindableProperty property, CancellationToken token, params Func<double, bool>[] conditions)
	{
		var conditionsMet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var nextCondition = 0;

		view.PropertyChanged += HandlePropertyChanged;

		return conditionsMet.Task.WaitAsync(waitTimeout, token);

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == property.PropertyName && conditions[nextCondition]((double)view.GetValue(property)) && ++nextCondition == conditions.Length)
			{
				view.PropertyChanged -= HandlePropertyChanged;
				conditionsMet.SetResult();
			}
		}
	}

	protected static Task WaitForAnyChangeAsync(VisualElement view, double[] baseline, CancellationToken token)
	{
		var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		view.PropertyChanged += HandlePropertyChanged;

		return changed.Task.WaitAsync(waitTimeout, token);

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (HasAnyValueChanged(CaptureValues(view), baseline))
			{
				view.PropertyChanged -= HandlePropertyChanged;
				changed.TrySetResult();
			}
		}
	}

	protected static double[] CaptureValues(VisualElement view) => [.. animatableProperties.Select(property => (double)view.GetValue(property.Property))];

	static bool HasAnyValueChanged(double[] currentValues, double[] baseline) =>
		currentValues.Zip(baseline).Any(pair => Math.Abs(pair.First - pair.Second) > precision);

	protected static void AssertValuesEqual(VisualElement view, double[] expectedValues)
	{
		var actualValues = CaptureValues(view);

		for (var i = 0; i < animatableProperties.Length; i++)
		{
			actualValues[i].Should().BeApproximately(expectedValues[i], precision, animatableProperties[i].Name);
		}
	}

	// AnimationBehavior starts its animations without handing out their Task, so a test cannot await them.
	protected sealed class EndedAnimations
	{
		readonly Channel<bool> endedAnimations = Channel.CreateUnbounded<bool>();

		public void NotifyEnded() => endedAnimations.Writer.TryWrite(true);

		public async Task WaitAsync(int count, CancellationToken token)
		{
			for (var i = 0; i < count; i++)
			{
				await endedAnimations.Reader.ReadAsync(token).AsTask().WaitAsync(waitTimeout, token);
			}
		}
	}

	protected sealed class ExtremeValueRecorder
	{
		readonly VisualElement view;
		readonly BindableProperty property;

		public ExtremeValueRecorder(VisualElement view, BindableProperty property)
		{
			this.view = view;
			this.property = property;
			Min = Max = (double)view.GetValue(property);
			view.PropertyChanged += HandlePropertyChanged;
		}

		public double Min { get; private set; }
		public double Max { get; private set; }

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != property.PropertyName)
			{
				return;
			}

			var value = (double)view.GetValue(property);
			Min = Math.Min(Min, value);
			Max = Math.Max(Max, value);
		}
	}
}
