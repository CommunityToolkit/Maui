using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests;
public class BehaviorTest
{
	[Fact]
	public void SuccessfulTest()
	{
		Assert.True(true);
	}

	[Fact]
	public void SomeBehavior()
	{
		var behavior = new IconTintColorBehavior();
		var expectedColor = Colors.Fuchsia;
		behavior.TintColor = expectedColor;

		var appliedColor = behavior.GetValue(IconTintColorBehavior.TintColorProperty) as Color;

		Assert.Equal(expectedColor, appliedColor);
	}
}
