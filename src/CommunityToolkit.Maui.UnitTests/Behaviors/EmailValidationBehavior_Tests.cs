using System.Threading.Tasks;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Controls;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class EmailValidationBehavior_Tests
{
	// Data from https://codefool.tumblr.com/post/15288874550/list-of-valid-and-invalid-email-addresses
	[Theory]
	[InlineData(@"email@example.com", true)]
	[InlineData(@"firstname.lastname@example.com", true)]
	[InlineData(@"email@subdomain.example.com", true)]
	[InlineData(@"firstname+lastname @example.com", true)]
	[InlineData(@"email@123.123.123.123", true)]
	[InlineData(@"email@[123.123.123.123]", true)]
	[InlineData(@"""email""@example.com", true)]
	[InlineData(@"1234567890@example.com", true)]
	[InlineData(@"email@example-one.com", true)]
	[InlineData(@"_______@example.com", true)]
	[InlineData(@"email@example.name", true)]
	[InlineData(@"email@example.museum", true)]
	[InlineData(@"email@example.co.jp", true)]
	[InlineData(@"firstname-lastname@example.com", true)]
	[InlineData(@"much.""more\ unusual""@example.com", true)]
	[InlineData(@"very.unusual.""@"".unusual.com@example.com", true)]
	[InlineData(@"very.""(),:;<>[]"".VERY.""very@\\ ""very"".unusual@strange.example.com", true)]
	[InlineData(@"plainaddress", false)]
	[InlineData(@"#@%^%#$@#$@#.com", false)]
	[InlineData(@"@example.com", false)]
	[InlineData(@"Joe Smith<email@example.com>", false)]
	[InlineData(@"email.example.com", false)]
	[InlineData(@"email@example@example.com", false)]
	[InlineData(@".email@example.com", false)]
	[InlineData(@"email.@example.com", false)]
	[InlineData(@"email..email@example.com", false)]
	[InlineData(@"あいうえお@example.com", false)]
	[InlineData(@"email@example.com (Joe Smith)", false)]
	[InlineData(@"email@example", false)]
	[InlineData(@"email@-example.com", false)]
	[InlineData(@"email@example.web", false)]
	[InlineData(@"email@111.222.333.44444", false)]
	[InlineData(@"email@example..com", false)]
	[InlineData(@"Abc..123@example.com", false)]
	[InlineData(@"""(),:;<>[\]", false)]
	[InlineData(@"just""not""right@example.com", false)]
	[InlineData(@"this\ is""really""not\allowed@example.co", false)]
	[InlineData("", false)]
	[InlineData(null, false)]
	public async Task IsValid(string? value, bool expectedValue)
	{
		// Arrange
		var behavior = new EmailValidationBehavior();

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
