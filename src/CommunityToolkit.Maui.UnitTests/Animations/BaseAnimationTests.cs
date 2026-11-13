using System.ComponentModel;
using System.Threading.Channels;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public abstract class BaseAnimationTests<TAnimation> : BaseAnimationTest where TAnimation : BaseAnimation, new()
{
	static readonly string[] animationHandles =
	[
		KeyframeAnimationExtensions.AnimationHandle,
		nameof(Microsoft.Maui.Controls.ViewExtensions.FadeToAsync),
		nameof(Microsoft.Maui.Controls.ViewExtensions.ScaleToAsync),
		nameof(Microsoft.Maui.Controls.ViewExtensions.RotateToAsync),
		nameof(Microsoft.Maui.Controls.ViewExtensions.RotateXToAsync),
		nameof(Microsoft.Maui.Controls.ViewExtensions.RotateYToAsync),
		nameof(Microsoft.Maui.Controls.ViewExtensions.TranslateToAsync)
	];

	// Each Commit ends on the first 16 ms mock tick at or after its length; FadeAnimation, ScaleAnimation and the flips commit twice
	const uint maximumTickRoundingMilliseconds = 2 * 16;

	const uint customTimingTestLength = 500;

	protected virtual TAnimation CreateAnimation() => new();

	protected TAnimation CreateFunctionalAnimation()
	{
		var animation = CreateAnimation();
		animation.Length = Math.Min(animation.Length, functionalTestLength);

		return animation;
	}

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

		foreach (var handle in animationHandles)
		{
			view.AnimationIsRunning(handle).Should().BeFalse($"a cancelled animation stops animating, but {handle} still runs");
		}
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

		firstAnimation.IsCompletedSuccessfully.Should().BeTrue();
		AssertValuesEqual(view, startingValues);

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
}
