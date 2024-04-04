using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Platform;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class EmailValidationBehaviorTests() : BaseBehaviorTest<EmailValidationBehavior, VisualElement>(new EmailValidationBehavior(), new View())
{
	public static IReadOnlyList<object[]> NonDefaultKeyboardData { get; } =
	[
		[Keyboard.Plain],
		[Keyboard.Numeric],
		[Keyboard.Chat],
		[Keyboard.Telephone],
		[Keyboard.Text],
		[Keyboard.Url],
	];

	// Data from https://codefool.tumblr.com/post/15288874550/list-of-valid-and-invalid-email-addresses
	public static IReadOnlyList<object?[]> ForceValidateData { get; } =
	[
		[@"email@example.com", true],
		[@"firstname.lastname@example.com", true],
		[@"email@subdomain.example.com", true],
		[@"firstname+lastname@example.com", true],
		[@"email@123.123.123.123", true],
		[@"email@[123.123.123.123]", true],
		[@"email@[IPv6:2001:db8:3333:4444:5555:6666:7777:8888]", true],
		[@"""email""@example.com", true],
		[@"1234567890@example.com", true],
		[@"email@example-one.com", true],
		[@"_______@example.com", true],
		[@"email@example.name", true],
		[@"email@example.museum", true],
		[@"email@example.co.jp", true],
		[@"firstname-lastname@example.com", true],
		[@"email@😃.example-one.com", true],
		[@"email@IPv6:2001:db8:3333:4444:5555:6666:7777:8888", false],
		[@"email@2001:db8:3333:4444:5555:6666:7777:8888", false],
		[@"plainaddress", false],
		[@"#@%^%#$@#$@#.com", false],
		[@"@example.com", false],
		[@"Joe Smith<email@example.com>", false],
		[@"email.example.com", false],
		[@"email@example@example.com", false],
		[@".email@example.com", false],
		[@"email.@example.com", false],
		[@"email..email@example.com", false],
		[@"email@example.com (Joe Smith)", false],
		[@"email@example", false],
		[@"email@-example.com", false],
		[@"email@111.222.333.44444", false],
		[@"email@example..com", false],
		[@"Abc..123@example.com", false],
		[@"""(),:;<>[\]", false],
		[@"this\ is""really""not\allowed@example.co", false],
		["", false],
	];

	public static IReadOnlyList<object?[]> EmailRegexTestData { get; } =
	[
		[@"email@example.com", true],
		[@"firstname.lastname@example.com", true],
		[@"email@subdomain.example.com", true],
		[@"firstname+lastname@example.com", true],
		[@"email@123.123.123.123", true],
		[@"email@[123.123.123.123]", true],
		[@"""email""@example.com", true],
		[@"1234567890@example.com", true],
		[@"email@example-one.com", true],
		[@"_______@example.com", true],
		[@"email@example.name", true],
		[@"email@example.museum", true],
		[@"email@example.co.jp", true],
		[@"firstname-lastname@example.com", true],
		[@"email@😃.example-one.com", true],
		[@"plainaddress", false],
		[@"#@%^%#$@#$@#.com", false],
		[@"@example.com", false],
		[@"Joe Smith<email@example.com>", false],
		[@"email.example.com", false],
		[@"email@example@example.com", false],
		[@"email@example.com (Joe Smith)", false],
		[@"email@example", false],
		[@"""(),:;<>[\]", false],
		[@"this\ is""really""not\allowed@example.co", false],
		["", false],
	];

	public static IReadOnlyList<object?[]> NonAsciiEmailAddress { get; } =
	[
		["example@😃.co.jp", "example@xn--h28h.co.jp"],
		["example@≡.com", "example@xn--2ch.com"],
		["example@111.222.111.222", "example@111.222.111.222"],
		["firstname@example.com", "firstname@example.com"],
	];

	[Theory]
	[MemberData(nameof(ForceValidateData))]
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
		await behavior.ForceValidate(CancellationToken.None);

		// Assert
		Assert.Equal(expectedValue, behavior.IsValid);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ForceValidateCancellationTokenExpired()
	{
		// Arrange
		var behavior = new EmailValidationBehavior();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var entry = new Entry
		{
			Text = "Hello"
		};
		entry.Behaviors.Add(behavior);

		// Act

		// Ensure CancellationToken expires 
		await Task.Delay(100, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () => await behavior.ForceValidate(cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ForceValidateCancellationTokenCanceled()
	{
		// Arrange
		var behavior = new EmailValidationBehavior();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var entry = new Entry
		{
			Text = "Hello"
		};
		entry.Behaviors.Add(behavior);

		// Act

		// Ensure CancellationToken expires 
		await cts.CancelAsync();

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () => await behavior.ForceValidate(cts.Token));
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

	[Theory]
	[MemberData(nameof(EmailRegexTestData))]
	public void EnsureValidEmailRegex(string email, bool expectedValue)
	{
		// Act
		var emailWithNormalizedDomain = CustomEmailValidationBehavior.EmailDomainRegex().Replace(email, CustomEmailValidationBehavior.DomainMapper);

		// Assert
		Assert.Equal(expectedValue, CustomEmailValidationBehavior.EmailRegex().IsMatch(emailWithNormalizedDomain));
	}

	[Fact]
	public void NullEmailRegexThrowsArgumentNullException()
	{
		// Assign
		string? nullInputString = null;

		// Assert
#pragma warning disable CS8604 // Possible null reference argument.
		Assert.Throws<ArgumentNullException>(() => CustomEmailValidationBehavior.EmailRegex().IsMatch(nullInputString));
#pragma warning restore CS8604 // Possible null reference argument.
	}

	[Theory]
	[MemberData(nameof(NonAsciiEmailAddress))]
	public void EnsureValidDomainRegex(string emailAddress, string expectedResult)
	{
		// Act
		var replaceResult = CustomEmailValidationBehavior.EmailDomainRegex().Replace(emailAddress, CustomEmailValidationBehavior.DomainMapper);

		// Assert
		Assert.Equal(expectedResult, replaceResult);
	}

	[Fact]
	public void NullDomainRegexThrowsArgumentNullException()
	{
		// Assign
		string? nullInputString = null;

		// Assert
#pragma warning disable CS8604 // Possible null reference argument.
		Assert.Throws<ArgumentNullException>(() => CustomEmailValidationBehavior.EmailRegex().IsMatch(nullInputString));
#pragma warning restore CS8604 // Possible null reference argument.
	}

	class CustomEmailValidationBehavior : EmailValidationBehavior
	{
		public static new Regex EmailRegex() => EmailValidationBehavior.EmailRegex();
		public static new Regex EmailDomainRegex() => EmailValidationBehavior.EmailDomainRegex();
		public static new string DomainMapper(Match match) => EmailValidationBehavior.DomainMapper(match);
	}
}