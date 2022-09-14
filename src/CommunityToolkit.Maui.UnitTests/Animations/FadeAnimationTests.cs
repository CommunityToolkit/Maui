using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class FadeAnimationTests : BaseTest
{
	[Fact]
	public async Task AnimateShouldThrowWithNullView()
	{
		FadeAnimation animation = new();

		var performAnimation = () => animation.Animate(null!);

		await performAnimation.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task AnimateShouldReturnToOriginalOpacity()
	{
		FadeAnimation animation = new();

		var label = new Label
		{
			Opacity = 0.9
		};
		label.EnableAnimations();

		await animation.Animate(label);

		label.Opacity.Should().Be(0.9);
	}
}