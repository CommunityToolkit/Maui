using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class FadeAnimationTests : BaseAnimationTests<FadeAnimation>
{
	const double originalOpacity = 0.9;
	const double fadedOpacity = 0.2;

	const uint fadeLength = 500;

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

		await WaitForCancellationAsync(cts.Token, TestContext.Current.CancellationToken);
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

		var fadingOut = WaitForPropertyAsync(label, VisualElement.OpacityProperty, token, opacity => opacity < originalOpacity - 0.2);
		var firstFade = animation.Animate(label, token);
		await fadingOut;

		var fadedAgain = WaitForPropertyAsync(label, VisualElement.OpacityProperty, token, opacity => opacity <= fadedOpacity + precision);
		var secondFade = animation.Animate(label, token);
		await Task.WhenAll(firstFade, secondFade).WaitAsync(waitTimeout, token);

		await fadedAgain;
		label.Opacity.Should().BeApproximately(originalOpacity, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimateStartedWhileFadingBackReturnsToOriginalOpacity() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
		var label = CreateAnimatedLabel();

		var fadingBack = WaitForPropertyAsync(label, VisualElement.OpacityProperty, token, opacity => opacity <= fadedOpacity + precision, opacity => opacity >= fadedOpacity + 0.3);
		var firstFade = animation.Animate(label, token);
		await fadingBack;

		var fadedAgain = WaitForPropertyAsync(label, VisualElement.OpacityProperty, token, opacity => opacity <= fadedOpacity + precision);
		var secondFade = animation.Animate(label, token);
		await Task.WhenAll(firstFade, secondFade).WaitAsync(waitTimeout, token);

		await fadedAgain;
		label.Opacity.Should().BeApproximately(originalOpacity, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimateStartedAfterAnInterruptedAnimationReturnsToOriginalOpacity() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
		var label = CreateAnimatedLabel();

		var fadingBack = WaitForPropertyAsync(label, VisualElement.OpacityProperty, token, opacity => opacity <= fadedOpacity + precision, opacity => opacity >= fadedOpacity + 0.3);
		var interruptedFade = animation.Animate(label, token);
		await fadingBack;

		label.CancelAnimations();
		await interruptedFade.WaitAsync(waitTimeout, token);
		var interruptedOpacity = label.Opacity;

		await animation.Animate(label, token).WaitAsync(waitTimeout, token);

		interruptedOpacity.Should().BeLessThan(originalOpacity - precision, "the interrupted animation leaves the label partially faded");
		label.Opacity.Should().BeApproximately(originalOpacity, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimateStartedAfterTheOpacityChangedReturnsToTheNewOpacity() => RunOnSingleThreadAsync(async token =>
	{
		const double newOpacity = 0.5;

		var animation = new FadeAnimation { Opacity = fadedOpacity, Length = fadeLength };
		var label = CreateAnimatedLabel();

		var fadingBack = WaitForPropertyAsync(label, VisualElement.OpacityProperty, token, opacity => opacity <= fadedOpacity + precision, opacity => opacity >= fadedOpacity + 0.3);
		var interruptedFade = animation.Animate(label, token);
		await fadingBack;

		label.CancelAnimations();
		await interruptedFade.WaitAsync(waitTimeout, token);

		label.Opacity = newOpacity;
		await animation.Animate(label, token).WaitAsync(waitTimeout, token);

		label.Opacity.Should().BeApproximately(newOpacity, precision);
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

		label.Opacity.Should().BeApproximately(originalOpacity, precision);
		otherLabel.Opacity.Should().BeApproximately(otherOriginalOpacity, precision);
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

		var fadingOut = WaitForPropertyAsync(border, VisualElement.OpacityProperty, token, opacity => opacity < originalOpacity - 0.2);
		behavior.AnimateCommand.Execute(token);
		await fadingOut;

		// AnimationBehavior cancels the running animation and starts a new one, so two animations end
		behavior.AnimateCommand.Execute(token);
		await animation.WaitForEndedAnimationsAsync(2, token);

		border.Opacity.Should().BeApproximately(originalOpacity, precision);

		behavior.AnimateCommand.Execute(token);
		await animation.WaitForEndedAnimationsAsync(1, token);

		border.Opacity.Should().BeApproximately(originalOpacity, precision);
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
		Assert.Equal(BindingMode.OneWay, FadeAnimation.OpacityProperty.DefaultBindingMode);
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void Opacity_NonFiniteValueIsIgnored(double nonFiniteValue)
	{
		var animation = new FadeAnimation { Opacity = nonFiniteValue };

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

	sealed class ObservedFadeAnimation : FadeAnimation
	{
		readonly EndedAnimations endedAnimations = new();

		public Task WaitForEndedAnimationsAsync(int count, CancellationToken token) => endedAnimations.WaitAsync(count, token);

		public override async Task Animate(VisualElement view, CancellationToken token = default)
		{
			try
			{
				await base.Animate(view, token);
			}
			finally
			{
				endedAnimations.NotifyEnded();
			}
		}
	}
}