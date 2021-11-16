using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Maui.Controls;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class RequiredStringValidationBehavior_Tests : BaseTest
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
