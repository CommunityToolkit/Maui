using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class FlipHorizontalAnimationTests : BaseAnimationTests<FlipHorizontalAnimation>
{
	const double originalRotationY = 20;

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new FlipHorizontalAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(FlipHorizontalAnimationDefaults.Length, animation.Length);
		Assert.Equal(FlipHorizontalAnimationDefaults.Rotation, animation.Rotation);
		Assert.Equal(600u, animation.Length);
		Assert.Equal(90.0, animation.Rotation);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ReachesRotationYOfRotation() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new FlipHorizontalAnimation();
		var view = CreateAnimatedView();

		var rotations = new ExtremeValueRecorder(view, VisualElement.RotationYProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		rotations.Max.Should().BeApproximately(animation.Rotation, precision);
		view.RotationY.Should().BeApproximately(originalRotationY, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HonorsCustomRotation() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new FlipHorizontalAnimation { Rotation = 180 };
		var view = CreateAnimatedView();

		var rotations = new ExtremeValueRecorder(view, VisualElement.RotationYProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		rotations.Max.Should().BeApproximately(180, precision);
		view.RotationY.Should().BeApproximately(originalRotationY, precision);
	}, TestContext.Current.CancellationToken);

	static Label CreateAnimatedView()
	{
		var label = new Label
		{
			RotationY = originalRotationY
		};
		label.EnableAnimations();

		return label;
	}
}
