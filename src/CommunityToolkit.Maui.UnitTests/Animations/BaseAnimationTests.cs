using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class BaseAnimationTests
{
	[Fact]
	public void BaseAnimationT_EnsureDefaults()
	{
		// Arrange
		BaseAnimation animation = new MockBaseAnimation();

		// Act // Assert
		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(BaseAnimationDefaults.Length, animation.Length);
	}

	[Fact]
	public void BaseAnimation_EnsureDefaults()
	{
		// Arrange
		BaseAnimation<VisualElement> animation = new MockBaseAnimation();

		// Act // Assert
		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(BaseAnimationDefaults.Length, animation.Length);
	}

	class MockBaseAnimation : BaseAnimation
	{
		public override Task Animate(VisualElement view, CancellationToken token = default)
		{
			throw new NotImplementedException();
		}
	}
}