﻿using System.Globalization;
using CommunityToolkit.Maui.Behaviors;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class NumericValidationBehaviorTests : BaseTest
{
	[Theory]
	[InlineData("en-US", "15.2", 1.0, 16.0, 0, 16, true)]
	[InlineData("en-US", "15.", 1.0, 16.0, 0, 1, true)]
	[InlineData("en-US", "15.88", 1.0, 16.0, 2, 2, true)]
	[InlineData("en-US", "0.99", 0.9, 2.0, 0, 16, true)]
	[InlineData("en-US", ".99", 0.9, 2.0, 0, 16, true)]
	[InlineData("en-US", "1,115.2", 1.0, 2000.0, 0, 16, true)]
	[InlineData("de-DE", "15,2", 1.0, 16.0, 0, 16, true)]
	[InlineData("de-DE", "15,", 1.0, 16.0, 0, 1, true)]
	[InlineData("de-DE", "15,88", 1.0, 16.0, 2, 2, true)]
	[InlineData("de-DE", "0,99", 0.9, 2.0, 0, 16, true)]
	[InlineData("de-DE", ",99", 0.9, 2.0, 0, 16, true)]
	[InlineData("de-DE", "1.115,2", 1.0, 2000.0, 0, 16, true)]
	[InlineData("en-US", "15.3", 16.0, 20.0, 0, 16, false)]
	[InlineData("en-US", "15.3", 0.0, 15.0, 0, 16, false)]
	[InlineData("en-US", "15.", 1.0, 16.0, 0, 0, false)]
	[InlineData("en-US", ".7", 0.0, 16.0, 0, 0, false)]
	[InlineData("en-US", "15", 1.0, 16.0, 1, 16, false)]
	[InlineData("en-US", "", 0.0, 16.0, 0, 16, false)]
	[InlineData("en-US", " ", 0.0, 16.0, 0, 16, false)]
	[InlineData("en-US", "15,2", 1.0, 16.0, 0, 16, false)]
	[InlineData("en-US", "1.115,2", 1.0, 2000.0, 0, 16, false)]
	[InlineData("de-DE", "15,3", 16.0, 20.0, 0, 16, false)]
	[InlineData("de-DE", "15,3", 0.0, 15.0, 0, 16, false)]
	[InlineData("de-DE", "15,", 1.0, 16.0, 0, 0, false)]
	[InlineData("de-DE", ",7", 0.0, 16.0, 0, 0, false)]
	[InlineData("de-DE", "15", 1.0, 16.0, 1, 16, false)]
	[InlineData("de-DE", "", 0.0, 16.0, 0, 16, false)]
	[InlineData("de-DE", " ", 0.0, 16.0, 0, 16, false)]
	[InlineData("de-DE", "15.2", 1.0, 16.0, 0, 16, false)]
	[InlineData("de-DE", "1,115.2", 1.0, 2000.0, 0, 16, false)]
	public async Task IsValid(string culture, string? value, double minValue, double maxValue, int minDecimalPlaces, int maxDecimalPlaces, bool expectedValue)
	{
		// Arrange
		var origCulture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

		var behavior = new NumericValidationBehavior
		{
			MinimumValue = minValue,
			MaximumValue = maxValue,
			MinimumDecimalPlaces = minDecimalPlaces,
			MaximumDecimalPlaces = maxDecimalPlaces
		};

		var entry = new Entry
		{
			Text = value
		};
		entry.Behaviors.Add(behavior);

		try
		{
			// Act
			await behavior.ForceValidate();

			// Assert
			Assert.Equal(expectedValue, behavior.IsValid);
		}
		finally
		{
			CultureInfo.CurrentCulture = origCulture;
		}
	}

	[Fact]
	public async Task IsNull()
	{
		// Arrange
		string? text = null;

		var behavior = new NumericValidationBehavior();

		var entry = new Entry
		{
			Text = text
		};
		entry.Behaviors.Add(behavior);

		await Assert.ThrowsAsync<ArgumentNullException>(async () => await behavior.ForceValidate());
	}

	[Fact]
	public async Task ShouldNotThrowIsNull()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInBehaviors(true);

		// Arrange
		string? text = null;

		var behavior = new NumericValidationBehavior();

		var entry = new Entry
		{
			Text = text
		};
		entry.Behaviors.Add(behavior);

		var action = (async () => await behavior.ForceValidate());
		await action.Should().NotThrowAsync<ArgumentNullException>();

		options.SetShouldSuppressExceptionsInBehaviors(false);
	}
}