using System.Globalization;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class RequiredStringValidationBehaviorTests() : BaseBehaviorTest<RequiredStringValidationBehavior, VisualElement>(new RequiredStringValidationBehavior(), new MockView())
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

	// RequiredString is bound to the text of the password Entry, and typing a new password takes the focus from the confirmation Entry.
	[Fact]
	public void IsValid_ShouldRevalidate_WhenRequiredStringChangesAfterTheViewLostFocus()
	{
		var passwordEntry = new Entry
		{
			Text = "123456"
		};

		var confirmPasswordBehavior = new RequiredStringValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnValueChanged
		};
		confirmPasswordBehavior.SetBinding(RequiredStringValidationBehavior.RequiredStringProperty, new Binding(nameof(Entry.Text), source: passwordEntry));

		var confirmPasswordEntry = new Entry();
		confirmPasswordEntry.Behaviors.Add(confirmPasswordBehavior);

		((IView)confirmPasswordEntry).IsFocused = true;
		confirmPasswordEntry.Text = "123456";
		((IView)confirmPasswordEntry).IsFocused = false;

		Assert.True(confirmPasswordBehavior.IsValid);

		passwordEntry.Text = "1234567";

		Assert.False(confirmPasswordBehavior.IsValid);

		passwordEntry.Text = "123456";

		Assert.True(confirmPasswordBehavior.IsValid);
	}

	[Fact]
	public void IsValid_ShouldRevalidate_WhenExactMatchChanges()
	{
		var behavior = new RequiredStringValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnValueChanged,
			RequiredString = "345"
		};

		var entry = new Entry();
		entry.Behaviors.Add(behavior);

		((IView)entry).IsFocused = true;
		entry.Text = "123456";

		Assert.False(behavior.IsValid);

		behavior.ExactMatch = false;

		Assert.True(behavior.IsValid);

		behavior.ExactMatch = true;

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public void VerifyDefaults()
	{
		// Arrange
		var requiredStringValidationBehavior = new RequiredStringValidationBehavior();

		// Act Assert
		Assert.Equal(RequiredStringValidationBehaviorDefaults.ExactMatch, requiredStringValidationBehavior.ExactMatch);
		Assert.Equal(RequiredStringValidationBehaviorDefaults.RequiredString, requiredStringValidationBehavior.RequiredString);
	}
}