using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class IconTintColorBehaviorTests() : BaseBehaviorTest<IconTintColorBehavior, View>(new IconTintColorBehavior(), new View())
{
	[Fact]
	public void VerifyDefaultColor()
	{
		var iconTintColorBehavior = new IconTintColorBehavior();

		Assert.Equal(default, iconTintColorBehavior.TintColor);
		Assert.Null(iconTintColorBehavior.TintColor);
	}

	[Fact]
	public void VerifyColorChanged()
	{
		var iconTintColorBehavior = new IconTintColorBehavior();

		Assert.Equal(default, iconTintColorBehavior.TintColor);

		iconTintColorBehavior.TintColor = Colors.Blue;

		Assert.Equal(Colors.Blue, iconTintColorBehavior.TintColor);
	}
}