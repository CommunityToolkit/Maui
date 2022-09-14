using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

#pragma warning disable CA1416 // Validate platform compatibility
public class IconTintColorBehaviorTests : BaseTest
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
#pragma warning restore CA1416 // Validate platform compatibility
}