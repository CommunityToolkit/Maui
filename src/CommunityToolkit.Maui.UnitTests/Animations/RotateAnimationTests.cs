using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class RotateAnimationTests : BaseAnimationTests<RotateAnimation>
{
	const double originalRotation = 30;

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new RotateAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(RotateAnimationDefaults.Length, animation.Length);
		Assert.Equal(RotateAnimationDefaults.Rotation, animation.Rotation);
		Assert.Equal(180.0, animation.Rotation);
		Assert.Equal(200u, animation.Length);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ReachesRotation() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new RotateAnimation();
		var view = CreateAnimatedView();
		var rotations = new ExtremeValueRecorder(view, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		rotations.Max.Should().BeApproximately(animation.Rotation, precision);
		view.Rotation.Should().BeApproximately(originalRotation, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HonorsCustomRotation() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new RotateAnimation { Rotation = 360 };
		var view = CreateAnimatedView();
		var rotations = new ExtremeValueRecorder(view, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		rotations.Max.Should().BeApproximately(360, precision);
		view.Rotation.Should().BeApproximately(originalRotation, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_RotateAndFlipHorizontalOnTheSameViewAtOnce_ReturnEachPropertyToItsOwnOriginalValue() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);

		await Task.WhenAll(new RotateAnimation().Animate(view, token), new FlipHorizontalAnimation().Animate(view, token)).WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_TriggeredByAnimationBehaviorWhileRotating_ReturnsToOriginalRotation() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ObservedRotateAnimation { Length = 500 };
		var behavior = new AnimationBehavior
		{
			AnimationType = animation
		};

		var border = new Border
		{
			Rotation = originalRotation,
			Behaviors = { behavior }
		};
		border.EnableAnimations();

		var rotating = WaitForPropertyAsync(border, VisualElement.RotationProperty, token, rotation => rotation > originalRotation + 20);
		behavior.AnimateCommand.Execute(token);
		await rotating;

		// AnimationBehavior cancels the running animation and starts a new one, so two animations end
		behavior.AnimateCommand.Execute(token);
		await animation.WaitForEndedAnimationsAsync(2, token);

		border.Rotation.Should().BeApproximately(originalRotation, precision);

		behavior.AnimateCommand.Execute(token);
		await animation.WaitForEndedAnimationsAsync(1, token);

		border.Rotation.Should().BeApproximately(originalRotation, precision);
	}, TestContext.Current.CancellationToken);

	static Label CreateAnimatedView()
	{
		var label = new Label
		{
			Rotation = originalRotation
		};
		label.EnableAnimations();

		return label;
	}

	sealed class ObservedRotateAnimation : RotateAnimation
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
