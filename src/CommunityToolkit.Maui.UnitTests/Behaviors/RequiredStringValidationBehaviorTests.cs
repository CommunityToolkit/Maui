using System.Globalization;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class RequiredStringValidationBehaviorTests() : BaseBehaviorTest<RequiredStringValidationBehavior, VisualElement>(new RequiredStringValidationBehavior(), new View())
{
	[Fact]
	public void IsValidTrueWhenBothIsNull_Test()
	{
		// Arrange
		var passwordEntry = new Entry();
		var confirmPasswordEntry = new Entry();
		var confirmPasswordBehavior = new RequiredStringValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnAttaching,
			RequiredString = passwordEntry.Text
		};

		// Act
		confirmPasswordEntry.Behaviors.Add(confirmPasswordBehavior);

		// Assert
		Assert.True(confirmPasswordBehavior.IsValid);
	}

	[Fact]
	public void IsValidFalseWhenOneIsNull_Test()
	{
		// Arrange
		var confirmPasswordEntry = new Entry();

		var passwordEntry = new Entry
		{
			Text = "123456"
		};
		var confirmPasswordBehavior = new RequiredStringValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnAttaching,
			RequiredString = passwordEntry.Text
		};

		// Act
		confirmPasswordEntry.Behaviors.Add(confirmPasswordBehavior);
		confirmPasswordEntry.Text = null;

		// Assert
		Assert.False(confirmPasswordBehavior.IsValid);
	}

	[Fact]
	public void IsValidTrueWhenEnterSameText_Test()
	{
		// Arrange
		var passwordEntry = new Entry
		{
			Text = "123456"
		};
		var confirmPasswordEntry = new Entry();
		var confirmPasswordBehavior = new RequiredStringValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnValueChanged,
			RequiredString = passwordEntry.Text
		};

		// Act
		confirmPasswordEntry.Behaviors.Add(confirmPasswordBehavior);
		confirmPasswordEntry.Text = "123456";

		// Assert
		Assert.True(confirmPasswordBehavior.IsValid);
	}

	// Based on helpful documentation found at: https://docs.microsoft.com/dotnet/api/system.stringcomparison?view=net-6.0#examples
	[Theory]
	[InlineData("en-US", false)] // US Culture where a != a-
	[InlineData("th-TH", true)] // Thai Culture where a == a-
	public void IsValidShouldReturnResultBasedOnCultureSpecificComparison(string cultureName, bool expectedIsValid)
	{
		var currentCulture = Thread.CurrentThread.CurrentCulture;

		try
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureName);

			// Arrange
			var passwordEntry = new Entry
			{
				Text = "a"
			};
			var confirmPasswordEntry = new Entry();
			var confirmPasswordBehavior = new RequiredStringValidationBehavior
			{
				Flags = ValidationFlags.ValidateOnValueChanged,
				RequiredString = passwordEntry.Text
			};

			// Act
			confirmPasswordEntry.Behaviors.Add(confirmPasswordBehavior);
			confirmPasswordEntry.Text = "a-";

			// Assert
			Assert.Equal(expectedIsValid, confirmPasswordBehavior.IsValid);
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = currentCulture;
		}
	}

	[Fact]
	public void IsValidTrueWhenExactMatchFalse_Test()
	{
		// Arrange
		var passwordEntry = new Entry
		{
			Text = "345"
		};
		var confirmPasswordEntry = new Entry();
		var confirmPasswordBehavior = new RequiredStringValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnValueChanged,
			RequiredString = passwordEntry.Text,
			ExactMatch = false
		};

		// Act
		confirmPasswordEntry.Behaviors.Add(confirmPasswordBehavior);
		confirmPasswordEntry.Text = "123456";

		// Assert
		Assert.True(confirmPasswordBehavior.IsValid);
	}

	[Fact]
	public void IsValidFalseWhenEnterDifferentText_Test()
	{
		// Arrange
		var confirmPasswordEntry = new Entry();

		var passwordEntry = new Entry
		{
			Text = "123456"
		};

		var confirmPasswordBehavior = new RequiredStringValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnValueChanged,
			RequiredString = passwordEntry.Text
		};

		// Act
		confirmPasswordEntry.Behaviors.Add(confirmPasswordBehavior);
		confirmPasswordEntry.Text = "1234567";

		// Assert
		Assert.False(confirmPasswordBehavior.IsValid);
	}
}