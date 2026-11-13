using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class ScaleAnimationTests : BaseAnimationTests<ScaleAnimation>
{
	const double originalScale = 0.8;

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new ScaleAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(ScaleAnimationDefaults.Length, animation.Length);
		Assert.Equal(ScaleAnimationDefaults.Scale, animation.Scale);
		Assert.Equal(340u, animation.Length);
		Assert.Equal(1.2, animation.Scale);
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void Scale_NonFiniteValueIsIgnored(double nonFiniteValue)
	{
		var animation = new ScaleAnimation { Scale = nonFiniteValue };

		Assert.Equal(ScaleAnimationDefaults.Scale, animation.Scale);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_PeaksAtScale() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ScaleAnimation();
		var view = CreateAnimatedView();

		var scales = new ExtremeValueRecorder(view, VisualElement.ScaleProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		scales.Max.Should().BeApproximately(animation.Scale, precision);
		view.Scale.Should().BeApproximately(originalScale, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HonorsCustomScale() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ScaleAnimation { Scale = 0.5 };
		var view = CreateAnimatedView();

		var scales = new ExtremeValueRecorder(view, VisualElement.ScaleProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		scales.Min.Should().BeApproximately(0.5, precision);
		view.Scale.Should().BeApproximately(originalScale, precision);
	}, TestContext.Current.CancellationToken);

	static Label CreateAnimatedView()
	{
		var label = new Label
		{
			Scale = originalScale
		};
		label.EnableAnimations();

		return label;
	}
}
