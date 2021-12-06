using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Controls;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class EmailValidationBehavior_Tests : BaseTest
{
	// Data from https://codefool.tumblr.com/post/15288874550/list-of-valid-and-invalid-email-addresses
	public static IReadOnlyList<object?[]> TestData { get; } = new[]
	{
		new object?[] { @"email@example.com", true },
		new object?[] { @"firstname.lastname@example.com", true },
		new object?[] { @"email@subdomain.example.com", true },
		new object?[] { @"firstname+lastname@example.com", true },
		new object?[] { @"email@123.123.123.123", true },
		new object?[] { @"email@[123.123.123.123]", true },
		new object?[] { @"""email""@example.com", true },
		new object?[] { @"1234567890@example.com", true },
		new object?[] { @"email@example-one.com", true },
		new object?[] { @"_______@example.com", true },
		new object?[] { @"email@example.name", true },
		new object?[] { @"email@example.museum", true },
		new object?[] { @"email@example.co.jp", true },
		new object?[] { @"firstname-lastname@example.com", true },
		new object?[] { @"plainaddress", false },
		new object?[] { @"#@%^%#$@#$@#.com", false },
		new object?[] { @"@example.com", false },
		new object?[] { @"Joe Smith<email@example.com>", false },
		new object?[] { @"email.example.com", false },
		new object?[] { @"email@example@example.com", false },
		new object?[] { @".email@example.com", false },
		new object?[] { @"email.@example.com", false },
		new object?[] { @"email..email@example.com", false },
		new object?[] { @"email@example.com (Joe Smith)", false },
		new object?[] { @"email@example", false },
		new object?[] { @"email@-example.com", false },
		new object?[] { @"email@111.222.333.44444", false },
		new object?[] { @"email@example..com", false },
		new object?[] { @"Abc..123@example.com", false },
		new object?[] { @"""(),:;<>[\]", false },
		new object?[] { @"this\ is""really""not\allowed@example.co", false },
		new object?[] { "", false },
		new object?[] { null, false },
	};

	[Theory]
	[MemberData(nameof(TestData))]
	public async Task IsValid(string? value, bool expectedValue)
	{
		// Arrange
		Console.WriteLine($"value: {value})");
		Console.WriteLine($"expectedValue: {expectedValue}");

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