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
        var confirmPasswordBehavior = new RequiredStringValidationBehavior();
        confirmPasswordBehavior.Flags = ValidationFlags.ValidateOnAttaching;

        // Act
        confirmPasswordBehavior.RequiredString = passwordEntry.Text;
        confirmPasswordEntry.Behaviors.Add(confirmPasswordBehavior);

        // Assert
        Assert.True(confirmPasswordBehavior.IsValid);
    }

    [Fact]
    public void IsValidFalseWhenOneIsNull_Test()
    {
        // Arrange
        var passwordEntry = new Entry();
        var confirmPasswordEntry = new Entry();
        var confirmPasswordBehavior = new RequiredStringValidationBehavior();
        confirmPasswordBehavior.Flags = ValidationFlags.ValidateOnAttaching;

        // Act
        passwordEntry.Text = "123456";
        confirmPasswordBehavior.RequiredString = passwordEntry.Text;
        confirmPasswordEntry.Behaviors.Add(confirmPasswordBehavior);

        confirmPasswordEntry.Text = null;

        // Assert
        Assert.False(confirmPasswordBehavior.IsValid);
    }

    [Fact]
    public void IsValidTrueWhenEnterSameText_Test()
    {
        // Arrange
        var passwordEntry = new Entry();
        var confirmPasswordEntry = new Entry();
        var confirmPasswordBehavior = new RequiredStringValidationBehavior();
        confirmPasswordBehavior.Flags = ValidationFlags.ValidateOnValueChanging;

        // Act
        passwordEntry.Text = "123456";
        confirmPasswordBehavior.RequiredString = passwordEntry.Text;
        confirmPasswordEntry.Behaviors.Add(confirmPasswordBehavior);
        confirmPasswordEntry.Text = "123456";

        // Assert
        Assert.True(confirmPasswordBehavior.IsValid);
    }

    [Fact]
    public void IsValidFalseWhenEnterDifferentText_Test()
    {
        // Arrange
        var passwordEntry = new Entry();
        var confirmPasswordEntry = new Entry();
        var confirmPasswordBehavior = new RequiredStringValidationBehavior();
        confirmPasswordBehavior.Flags = ValidationFlags.ValidateOnValueChanging;

        // Act
        passwordEntry.Text = "123456";
        confirmPasswordBehavior.RequiredString = passwordEntry.Text;
        confirmPasswordEntry.Behaviors.Add(confirmPasswordBehavior);
        confirmPasswordEntry.Text = "1234567";

        // Assert
        Assert.False(confirmPasswordBehavior.IsValid);
    }
}
