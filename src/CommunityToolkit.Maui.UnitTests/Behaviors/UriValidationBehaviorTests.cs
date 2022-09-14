using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class UriValidationBehaviorTests : BaseTest
{
	[Theory]
	[InlineData(@"http://microsoft.com", UriKind.Absolute, true)]
	[InlineData(@"microsoft/xamarin/news", UriKind.Relative, true)]
	[InlineData(@"http://microsoft.com", UriKind.RelativeOrAbsolute, true)]
	[InlineData(@"microsoftcom", UriKind.Absolute, false)]
	[InlineData(@"microsoft\\\\\xamarin/news", UriKind.Relative, false)]
	[InlineData(@"ht\\\.com", UriKind.RelativeOrAbsolute, false)]
	public async Task IsValid(string value, UriKind uriKind, bool expectedValue)
	{
		// Arrange
		var behavior = new UriValidationBehavior
		{
			UriKind = uriKind,
			Value = value
		};

		// Act
		await behavior.ForceValidate();

		// Assert
		Assert.Equal(expectedValue, behavior.IsValid);
	}
}