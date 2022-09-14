using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class EmailValidationBehaviorTests : BaseTest
{
	public static IReadOnlyList<object[]> NonDefaultKeyboardData { get; } = new[]
	{
		new[] { Keyboard.Plain },
		new[] { Keyboard.Numeric },
		new[] { Keyboard.Chat },
		new[] { Keyboard.Telephone },
		new[] { Keyboard.Text },
		new[] { Keyboard.Url },
	};

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

	[Fact]
	public void EnsureEntryEmailKeyboardWhenDefaultKeyboardAssigned()
	{
		// Arrange
		var behavior = new EmailValidationBehavior();

		var entry = new Entry
		{
			Text = "mabuta@gmail.com"
		};

		// Act 
		entry.Behaviors.Add(behavior);

		// Assert
		Assert.Equal(Keyboard.Email, entry.Keyboard);

		// Act
		entry.Behaviors.Remove(behavior);

		// Assert
		Assert.Equal(Keyboard.Default, entry.Keyboard);
	}

	[Fact]
	public void EnsureEditorEmailKeyboardWhenDefaultKeyboardAssigned()
	{
		// Arrange
		var behavior = new EmailValidationBehavior();

		var editor = new Editor
		{
			Text = "mabuta@gmail.com"
		};

		// Act 
		editor.Behaviors.Add(behavior);

		// Assert
		Assert.Equal(Keyboard.Email, editor.Keyboard);

		// Act
		editor.Behaviors.Remove(behavior);

		// Assert
		Assert.Equal(Keyboard.Default, editor.Keyboard);
	}

	[Theory]
	[MemberData(nameof(NonDefaultKeyboardData))]
	public void EnsureEntryKeyboardNotOverwritten(Keyboard keyboard)
	{
		// Arrange
		var behavior = new EmailValidationBehavior();

		var entry = new Entry
		{
			Text = "mabuta@gmail.com",
			Keyboard = keyboard
		};

		// Act 
		entry.Behaviors.Add(behavior);

		// Assert
		Assert.Equal(keyboard, entry.Keyboard);

		// Act
		entry.Behaviors.Remove(behavior);

		// Assert
		Assert.Equal(keyboard, entry.Keyboard);
	}

	[Theory]
	[MemberData(nameof(NonDefaultKeyboardData))]
	public void EnsureEditorEmailKeyboardNotOverwritten(Keyboard keyboard)
	{
		// Arrange
		var behavior = new EmailValidationBehavior();

		var editor = new Editor
		{
			Text = "mabuta@gmail.com",
			Keyboard = keyboard
		};

		// Act 
		editor.Behaviors.Add(behavior);

		// Assert
		Assert.Equal(keyboard, editor.Keyboard);

		// Act
		editor.Behaviors.Remove(behavior);

		// Assert
		Assert.Equal(keyboard, editor.Keyboard);
	}
}