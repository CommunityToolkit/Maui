using System.Text.RegularExpressions;
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
	public static IReadOnlyList<object?[]> EmailTestData { get; } = new[]
	{
		new object[] { @"email@example.com", true },
		new object[] { @"firstname.lastname@example.com", true },
		new object[] { @"email@subdomain.example.com", true },
		new object[] { @"firstname+lastname@example.com", true },
		new object[] { @"email@123.123.123.123", true },
		new object[] { @"email@[123.123.123.123]", true },
		new object[] { @"""email""@example.com", true },
		new object[] { @"1234567890@example.com", true },
		new object[] { @"email@example-one.com", true },
		new object[] { @"_______@example.com", true },
		new object[] { @"email@example.name", true },
		new object[] { @"email@example.museum", true },
		new object[] { @"email@example.co.jp", true },
		new object[] { @"firstname-lastname@example.com", true },
		new object[] { @"email@😃.example-one.com", true },
		new object[] { @"plainaddress", false },
		new object[] { @"#@%^%#$@#$@#.com", false },
		new object[] { @"@example.com", false },
		new object[] { @"Joe Smith<email@example.com>", false },
		new object[] { @"email.example.com", false },
		new object[] { @"email@example@example.com", false },
		new object[] { @".email@example.com", false },
		new object[] { @"email.@example.com", false },
		new object[] { @"email..email@example.com", false },
		new object[] { @"email@example.com (Joe Smith)", false },
		new object[] { @"email@example", false },
		new object[] { @"email@-example.com", false },
		new object[] { @"email@111.222.333.44444", false },
		new object[] { @"email@example..com", false },
		new object[] { @"Abc..123@example.com", false },
		new object[] { @"""(),:;<>[\]", false },
		new object[] { @"this\ is""really""not\allowed@example.co", false },
		new object[] { "", false },
	};

	// Valid Pulic IPv4 Ranges https://phoenixnap.com/kb/public-vs-private-ip-address
	public static IReadOnlyList<object?[]> IPv4Data { get; } = new[]
	{
		new object[] { "111.222.111.222", true },
		new object[] { "111.0.111.111", true },
		new object[] { "111.111.0.111", true },
		new object[] { "111.111.111.0", true },
		new object[] { "1.0.0.1", true },
		new object[] { "9.255.255.255", true },
		new object[] { "11.0.0.0", true },
		new object[] { "100.63.255.255", true },
		new object[] { "100.128.0.0", true },
		new object[] { "126.255.255.255", true },
		new object[] { "128.0.0.0", true },
		new object[] { "169.253.255.255", true },
		new object[] { "169.255.0.0", true },
		new object[] { "172.15.255.255", true },
		new object[] { "172.32.0.0", true },
		new object[] { "191.255.255.255", true },
		new object[] { "192.0.1.0", true },
		new object[] { "192.0.3.0", true },
		new object[] { "192.88.98.255", true },
		new object[] { "192.88.100.0", true },
		new object[] { "192.167.255.255", true },
		new object[] { "192.169.0.0", true },
		new object[] { "198.17.255.255", true },
		new object[] { "198.20.0.0", true },
		new object[] { "198.51.99.255", true },
		new object[] { "198.51.101.0", true },
		new object[] { "203.0.112.255", true },
		new object[] { "203.0.114.0", true },
		new object[] { "223.255.255.255", true },
		new object[] { "255.255.255.255", true },
		new object[] { "192.168.1.1", true },
		new object[] { "10.0.1.1", true },
		new object[] { "172.16.1.1", true },
		new object[] { "0.111.111.111", true },
		new object[] { "256.111.111.111", false },
		new object[] { "111.256.111.111", false },
		new object[] { "111.111.256.111", false },
		new object[] { "111.111.111.256", false },
		new object[] { "111.111.111", false },
		new object[] { "111.111.111.", false },
		new object[] { "111.111.111.111.", false },
		new object[] { "111.111.111.111.111", false },
		new object[] { "", false },
	};

	public static IReadOnlyList<object?[]> EmailRegexTestData { get; } = new[]
	{
		new object[] { @"email@example.com", true },
		new object[] { @"firstname.lastname@example.com", true },
		new object[] { @"email@subdomain.example.com", true },
		new object[] { @"firstname+lastname@example.com", true },
		new object[] { @"email@123.123.123.123", true },
		new object[] { @"email@[123.123.123.123]", true },
		new object[] { @"""email""@example.com", true },
		new object[] { @"1234567890@example.com", true },
		new object[] { @"email@example-one.com", true },
		new object[] { @"_______@example.com", true },
		new object[] { @"email@example.name", true },
		new object[] { @"email@example.museum", true },
		new object[] { @"email@example.co.jp", true },
		new object[] { @"firstname-lastname@example.com", true },
		new object[] { @"email@😃.example-one.com", true },
		new object[] { @"plainaddress", false },
		new object[] { @"#@%^%#$@#$@#.com", false },
		new object[] { @"@example.com", false },
		new object[] { @"Joe Smith<email@example.com>", false },
		new object[] { @"email.example.com", false },
		new object[] { @"email@example@example.com", false },
		new object[] { @"email@example.com (Joe Smith)", false },
		new object[] { @"email@example", false },
		new object[] { @"""(),:;<>[\]", false },
		new object[] { @"this\ is""really""not\allowed@example.co", false },
		new object[] { "", false },
	};

	public static IReadOnlyList<object?[]> SubdomainData { get; } = new[]
	{
		new object?[] { "example@😃.co.jp", "example@xn--h28h.co.jp" },
		new object?[] { "example@≡.com", "example@xn--2ch.com" },
		new object?[] { "example@111.222.111.222", "example@111.222.111.222" },
		new object?[] { "firstname@example.com", "firstname@example.com" },
	};

	[Theory]
	[MemberData(nameof(EmailTestData))]
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

	[Theory]
	[MemberData(nameof(IPv4Data))]
	public void EnsureValidIpv4Regex(string ipAddress, bool expectedResult)
	{
		// Assert
		Assert.Equal(expectedResult, CustomEmailValidationBehavior.Ipv4Regex().IsMatch(ipAddress));
	}

	[Fact]
	public void NullValidIpv4RegexThrowsArgumentNullException()
	{
		// Assign
		string? nullInputString = null;

		// Assert
#pragma warning disable CS8604 // Possible null reference argument.
		Assert.Throws<ArgumentNullException>(() => CustomEmailValidationBehavior.Ipv4Regex().IsMatch(nullInputString));
#pragma warning restore CS8604 // Possible null reference argument.
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
	[MemberData(nameof(SubdomainData))]
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
		public static new Regex Ipv4Regex() => EmailValidationBehavior.Ipv4Regex();
		public static new Regex Ipv6Regex() => EmailValidationBehavior.Ipv6Regex();
		public static new Regex EmailRegex() => EmailValidationBehavior.EmailRegex();
		public static new Regex EmailDomainRegex() => EmailValidationBehavior.EmailDomainRegex();
		public static new string DomainMapper(Match match) => EmailValidationBehavior.DomainMapper(match);
	}
}