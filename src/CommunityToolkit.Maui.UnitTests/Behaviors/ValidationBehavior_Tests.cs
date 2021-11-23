using System.Threading.Tasks;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class ValidationBehavior_Tests : BaseTest
{
	[Fact]
	public void ValidateOnValueChanged()
	{
		// Arrange
		var entry = new Entry
		{
			Text = "123"
		};
		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "321",
			Flags = ValidationFlags.ValidateOnValueChanged
		};

		entry.Behaviors.Add(behavior);

		// Act
		entry.Text = "321";

		// Assert
		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ValidValue_ValidStyle()
	{
		// Arrange
		var entry = new Entry
		{
			Text = "123"
		};
		var validStyle = new Style(entry.GetType());
		validStyle.Setters.Add(new Setter() { Property = Entry.BackgroundColorProperty, Value = Colors.Green });
		var invalidStyle = new Style(entry.GetType());
		invalidStyle.Setters.Add(new Setter() { Property = Entry.BackgroundColorProperty, Value = Colors.Red });
		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "321",
			ValidStyle = validStyle,
			InvalidStyle = invalidStyle
		};

		entry.Behaviors.Add(behavior);

		// Act
		entry.Text = "321";
		await behavior.ForceValidate();

		// Assert
		Assert.Equal(entry.Style, validStyle);
	}

	[Fact]
	public async Task InvalidValue_InvalidStyle()
	{
		// Arrange
		var entry = new Entry
		{
			Text = "123"
		};
		var validStyle = new Style(entry.GetType());
		validStyle.Setters.Add(new Setter() { Property = Entry.BackgroundColorProperty, Value = Colors.Green });
		var invalidStyle = new Style(entry.GetType());
		invalidStyle.Setters.Add(new Setter() { Property = Entry.BackgroundColorProperty, Value = Colors.Red });
		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "21",
			ValidStyle = validStyle,
			InvalidStyle = invalidStyle
		};

		entry.Behaviors.Add(behavior);

		// Act
		entry.Text = "321";
		await behavior.ForceValidate();

		// Assert
		Assert.Equal(entry.Style, invalidStyle);
	}

	[Fact]
	public void IsRunning()
	{
		// Arrange
		var entry = new Entry
		{
			Text = "123"
		};
		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "321",
			SimulateValidationDelay = true
		};

		entry.Behaviors.Add(behavior);

		// Act
		entry.Text = "321";
		Assert.False(behavior.IsRunning);
		behavior.ForceValidate();

		// Assert
		Assert.True(behavior.IsRunning);
	}

	[Fact]
	public void ForceValidateCommand()
	{
		// Arrange
		var entry = new Entry
		{
			Text = "123"
		};
		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "321",
			ForceValidateCommand = new Command(() =>
			{
				entry.Text = "321";
			})
		};

		entry.Behaviors.Add(behavior);

		// Act
		behavior.ForceValidateCommand.Execute(null);

		// Assert
		Assert.True(behavior.IsValid);
	}
}