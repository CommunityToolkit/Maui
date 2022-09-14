using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class TextValidationBehaviorTests : BaseTest
{
	[Theory]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 5, 25, TextDecorationFlags.None, "Microsoft", true)]
	[InlineData("mi.....ft", RegexOptions.None, 5, 25, TextDecorationFlags.Trim, "minecraft    ", true)]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 5, 25, TextDecorationFlags.None, "microservice", false)]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 5, 6, TextDecorationFlags.None, "Microsoft", false)]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 10, 11, TextDecorationFlags.None, "Microsoft", false)]
	public async Task IsValid(string pattern, RegexOptions options, int minLength, int maxLength, TextDecorationFlags flags, string value, bool expectedValue)
	{
		// Arrange
		var behavior = new TextValidationBehavior
		{
			RegexPattern = pattern,
			RegexOptions = options,
			MinimumLength = minLength,
			MaximumLength = maxLength,
			DecorationFlags = flags
		};

		var entry = new Entry
		{
			Text = value
		};
		entry.Behaviors.Add(behavior);

		// Act
		await behavior.ForceValidate();

		// Assert
		Assert.Equal(expectedValue, behavior.IsValid);
	}
}