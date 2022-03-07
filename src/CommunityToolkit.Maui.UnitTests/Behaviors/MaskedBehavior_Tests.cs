using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class MaskedBehavior_Tests : BaseTest
{
	[Theory]
	[InlineData(null, null, null)]
	public void ValidMaskTests(string? mask, string? input, string expectedResult)
	{
		var maskedBehavior = new MaskedBehavior
		{
			Mask = mask
		};

		var entry = new Entry
		{
			Behaviors = { maskedBehavior },
			Text = input
		};

		Assert.Equal(expectedResult, entry.Text);
	}
}

