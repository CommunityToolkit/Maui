using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class FlipVerticalAnimationTests : BaseAnimationTests<FlipVerticalAnimation>
{
	const double originalRotationX = 20;

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new FlipVerticalAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(FlipVerticalAnimationDefaults.Length, animation.Length);
		Assert.Equal(FlipVerticalAnimationDefaults.Rotation, animation.Rotation);
		Assert.Equal(600u, animation.Length);
		Assert.Equal(90.0, animation.Rotation);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ReachesRotationXOfRotation() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new FlipVerticalAnimation();
		var view = CreateAnimatedView();

		using var rotations = new ExtremeValueRecorder(view, VisualElement.RotationXProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		rotations.Max.Should().BeApproximately(animation.Rotation, precision);
		view.RotationX.Should().BeApproximately(originalRotationX, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HonorsCustomRotation() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new FlipVerticalAnimation { Rotation = 180 };
		var view = CreateAnimatedView();

		using var rotations = new ExtremeValueRecorder(view, VisualElement.RotationXProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		rotations.Max.Should().BeApproximately(180, precision);
		view.RotationX.Should().BeApproximately(originalRotationX, precision);
	}, TestContext.Current.CancellationToken);

	static Label CreateAnimatedView()
	{
		var label = new Label
		{
			RotationX = originalRotationX
		};
		label.EnableAnimations();

		return label;
	}
}
