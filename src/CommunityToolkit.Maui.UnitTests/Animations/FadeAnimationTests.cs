using System.ComponentModel;
using System.Threading.Channels;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class FadeAnimationTests : BaseTest
{
	const double originalOpacity = 0.9;
	const double fadedOpacity = 0.2;
	const double opacityPrecision = 0.001;

	// Each of the two fades of an animation (out, then back) lasts this long
	const uint fadeLength = 500;

	static readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(5);

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task AnimateShouldThrowWithNullView()
	{
		FadeAnimation animation = new();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => animation.Animate(null, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenCanceled()
	{
		FadeAnimation animation = new();
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

		var label = new Label
		{
			Opacity = 0.9
		};
		label.EnableAnimations();

		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
		{
			await cts.CancelAsync();
			await animation.Animate(label, cts.Token);
		});
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenExpired()
	{
		FadeAnimation animation = new();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var label = new Label
		{
			Opacity = 0.9
		};
		label.EnableAnimations();

		await Task.Delay(10, TestContext.Current.CancellationToken);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => animation.Animate(label, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task AnimateShouldReturnToOriginalOpacity()
	{
		FadeAnimation animation = new();

		var label = new Label
		{
			Opacity = 0.9
		};
		label.EnableAnimations();

		await animation.Animate(label, TestContext.Current.CancellationToken);

		label.Opacity.Should().Be(0.9);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimateStartedWhileFadingOutReturnsToOriginalOpacity() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
		var label = CreateAnimatedLabel();

		var fadingOut = WaitForOpacityAsync(label, token, opacity => opacity < originalOpacity - 0.2);
		var firstFade = animation.Animate(label, token);
		await fadingOut;

		var fadedAgain = WaitForOpacityAsync(label, token, opacity => opacity <= fadedOpacity + opacityPrecision);
		var secondFade = animation.Animate(label, token);
		await Task.WhenAll(firstFade, secondFade).WaitAsync(waitTimeout, token);

		fadedAgain.IsCompletedSuccessfully.Should().BeTrue("the second animation fades the label out before it fades it back");
		label.Opacity.Should().BeApproximately(originalOpacity, opacityPrecision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimateStartedWhileFadingBackReturnsToOriginalOpacity() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
		var label = CreateAnimatedLabel();

		var fadingBack = WaitForOpacityAsync(label, token, opacity => opacity <= fadedOpacity + opacityPrecision, opacity => opacity >= fadedOpacity + 0.3);
		var firstFade = animation.Animate(label, token);
		await fadingBack;

		var fadedAgain = WaitForOpacityAsync(label, token, opacity => opacity <= fadedOpacity + opacityPrecision);
		var secondFade = animation.Animate(label, token);
		await Task.WhenAll(firstFade, secondFade).WaitAsync(waitTimeout, token);

		fadedAgain.IsCompletedSuccessfully.Should().BeTrue("the second animation fades the label out before it fades it back");
		label.Opacity.Should().BeApproximately(originalOpacity, opacityPrecision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimateStartedAfterAnInterruptedAnimationReturnsToOriginalOpacity() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
		var label = CreateAnimatedLabel();

		var fadingBack = WaitForOpacityAsync(label, token, opacity => opacity <= fadedOpacity + opacityPrecision, opacity => opacity >= fadedOpacity + 0.3);
		var interruptedFade = animation.Animate(label, token);
		await fadingBack;

		label.CancelAnimations();
		await interruptedFade.WaitAsync(waitTimeout, token);
		var interruptedOpacity = label.Opacity;

		await animation.Animate(label, token).WaitAsync(waitTimeout, token);

		interruptedOpacity.Should().BeLessThan(originalOpacity - opacityPrecision, "the interrupted animation leaves the label partially faded");
		label.Opacity.Should().BeApproximately(originalOpacity, opacityPrecision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimateStartedAfterTheOpacityChangedReturnsToTheNewOpacity() => RunOnSingleThreadAsync(async token =>
	{
		const double newOpacity = 0.5;

		var animation = new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
		var label = CreateAnimatedLabel();

		var fadingBack = WaitForOpacityAsync(label, token, opacity => opacity <= fadedOpacity + opacityPrecision, opacity => opacity >= fadedOpacity + 0.3);
		var interruptedFade = animation.Animate(label, token);
		await fadingBack;

		label.CancelAnimations();
		await interruptedFade.WaitAsync(waitTimeout, token);

		label.Opacity = newOpacity;
		await animation.Animate(label, token).WaitAsync(waitTimeout, token);

		label.Opacity.Should().BeApproximately(newOpacity, opacityPrecision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimateReturnsEachViewToItsOwnOriginalOpacity() => RunOnSingleThreadAsync(async token =>
	{
		const double otherOriginalOpacity = 0.6;

		var animation = new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
		var label = CreateAnimatedLabel();
		var otherLabel = CreateAnimatedLabel(otherOriginalOpacity);

		var labelFade = animation.Animate(label, token);
		var otherLabelFade = animation.Animate(otherLabel, token);
		await Task.WhenAll(labelFade, otherLabelFade).WaitAsync(waitTimeout, token);

		label.Opacity.Should().BeApproximately(originalOpacity, opacityPrecision);
		otherLabel.Opacity.Should().BeApproximately(otherOriginalOpacity, opacityPrecision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimationBehaviorTriggeredWhileFadingReturnsToOriginalOpacity() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ObservedFadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
		var behavior = new AnimationBehavior
		{
			AnimationType = animation
		};

		var border = new Border
		{
			Opacity = originalOpacity,
			Behaviors = { behavior }
		};
		border.EnableAnimations();

		// AnimationBehavior cancels the running animation and starts a new one
		var fadingOut = WaitForOpacityAsync(border, token, opacity => opacity < originalOpacity - 0.2);
		behavior.AnimateCommand.Execute(token);
		await fadingOut;

		behavior.AnimateCommand.Execute(token);
		await animation.WaitForEndedAnimationsAsync(2, token);

		border.Opacity.Should().BeApproximately(originalOpacity, opacityPrecision);

		// A later animation that nothing interrupts returns the border to its original opacity as well
		behavior.AnimateCommand.Execute(token);
		await animation.WaitForEndedAnimationsAsync(1, token);

		border.Opacity.Should().BeApproximately(originalOpacity, opacityPrecision);
	}, TestContext.Current.CancellationToken);

	[Fact]
	public void EnsureDefaults()
	{
		// Arrange
		var animation = new FadeAnimation();

		// Act // Assert
		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(FadeAnimationDefaults.Length, animation.Length);
		Assert.Equal(FadeAnimationDefaults.Opacity, animation.Opacity);
	}

	static Label CreateAnimatedLabel(double opacity = originalOpacity)
	{
		var label = new Label
		{
			Opacity = opacity
		};
		label.EnableAnimations();

		return label;
	}

	// Runs a test on one thread, the way an app runs its animations on its UI thread. The mock ticker of EnableAnimations then fires on that
	// thread as well, instead of on the thread pool, where it races the test, and the tickers of other views, for MAUI's animation bookkeeping
	// (a static Dictionary that is not thread-safe).
	static Task RunOnSingleThreadAsync(Func<CancellationToken, Task> test, CancellationToken token) =>
		Task.Run(() => AsyncContext.Run(() => test(token)), token);

	// Completes once the opacity of the view, as it changes, has met each condition in turn
	static Task WaitForOpacityAsync(VisualElement view, CancellationToken token, params Func<double, bool>[] conditions)
	{
		var conditionsMet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var nextCondition = 0;

		view.PropertyChanged += HandlePropertyChanged;

		return conditionsMet.Task.WaitAsync(waitTimeout, token);

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

		public async Task WaitForEndedAnimationsAsync(int count, CancellationToken token)
		{
			for (var i = 0; i < count; i++)
			{
				await endedAnimations.Reader.ReadAsync(token).AsTask().WaitAsync(waitTimeout, token);
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