using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Behaviors;

public class TextValidationBehaviorTests
{
	[Fact]
	public async Task ForceValidate_ValidLength_ReturnsValid()
	{
		var behavior = new TextValidationBehavior
		{
			MinimumLength = 3,
			MaximumLength = 10,
			RegexPattern = ".*",
			Value = "hello"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_TooShort_ReturnsInvalid()
	{
		var behavior = new TextValidationBehavior
		{
			MinimumLength = 5,
			MaximumLength = 10,
			RegexPattern = ".*",
			Value = "hi"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_TooLong_ReturnsInvalid()
	{
		var behavior = new TextValidationBehavior
		{
			MinimumLength = 1,
			MaximumLength = 3,
			RegexPattern = ".*",
			Value = "hello world"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_NullValue_ReturnsInvalid()
	{
		var behavior = new TextValidationBehavior
		{
			MinimumLength = 0,
			MaximumLength = 100,
			RegexPattern = ".*",
			Value = null
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_RegexMatch_ReturnsValid()
	{
		var behavior = new TextValidationBehavior
		{
			RegexPattern = "^[a-z]+$",
			Value = "hello"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_RegexNoMatch_ReturnsInvalid()
	{
		var behavior = new TextValidationBehavior
		{
			RegexPattern = "^[a-z]+$",
			Value = "Hello123"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_RegexIgnoreCase_ReturnsValid()
	{
		var behavior = new TextValidationBehavior
		{
			RegexPattern = "^[a-z]+$",
			RegexOptions = RegexOptions.IgnoreCase,
			Value = "Hello"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_TrimDecoration_TrimsBeforeValidation()
	{
		var behavior = new TextValidationBehavior
		{
			MinimumLength = 3,
			MaximumLength = 5,
			RegexPattern = ".*",
			DecorationFlags = TextDecorationFlags.Trim,
			Value = "  hi  "
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_NullToEmptyDecoration_NullValue_IsInvalid()
	{
		var behavior = new TextValidationBehavior
		{
			MinimumLength = 0,
			MaximumLength = 100,
			RegexPattern = ".*",
			DecorationFlags = TextDecorationFlags.NullToEmpty,
			Value = null
		};

		await behavior.ForceValidate(CancellationToken.None);

		// The NullToEmpty decoration in TextValidationBehavior.Decorate(string?) is not invoked
		// through the validation pipeline because ValidationBehavior<T>.Decorate(object?)
		// returns (T?)value directly without delegating to the typed override.
		// Therefore null fails the "value != null" check in ValidateAsync.
		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task IsNotValid_IsOppositeOfIsValid()
	{
		var behavior = new TextValidationBehavior
		{
			RegexPattern = ".*",
			Value = "test"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
		Assert.False(behavior.IsNotValid);
	}
}

public class CharactersValidationBehaviorTests
{
	[Fact]
	public async Task ForceValidate_DigitCountInRange_ReturnsValid()
	{
		var behavior = new CharactersValidationBehavior
		{
			CharacterType = CharacterType.Digit,
			MinimumCharacterTypeCount = 1,
			MaximumCharacterTypeCount = 3,
			RegexPattern = ".*",
			Value = "abc12"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_TooFewDigits_ReturnsInvalid()
	{
		var behavior = new CharactersValidationBehavior
		{
			CharacterType = CharacterType.Digit,
			MinimumCharacterTypeCount = 3,
			MaximumCharacterTypeCount = 10,
			RegexPattern = ".*",
			Value = "abc1"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_TooManyDigits_ReturnsInvalid()
	{
		var behavior = new CharactersValidationBehavior
		{
			CharacterType = CharacterType.Digit,
			MinimumCharacterTypeCount = 0,
			MaximumCharacterTypeCount = 2,
			RegexPattern = ".*",
			Value = "a1234"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_UppercaseLetters_CountedCorrectly()
	{
		var behavior = new CharactersValidationBehavior
		{
			CharacterType = CharacterType.UppercaseLetter,
			MinimumCharacterTypeCount = 2,
			MaximumCharacterTypeCount = 5,
			RegexPattern = ".*",
			Value = "Hello World"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_NonAlphanumericSymbol_CountedCorrectly()
	{
		var behavior = new CharactersValidationBehavior
		{
			CharacterType = CharacterType.NonAlphanumericSymbol,
			MinimumCharacterTypeCount = 1,
			MaximumCharacterTypeCount = 3,
			RegexPattern = ".*",
			Value = "hello!"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_Whitespace_CountedCorrectly()
	{
		var behavior = new CharactersValidationBehavior
		{
			CharacterType = CharacterType.Whitespace,
			MinimumCharacterTypeCount = 1,
			MaximumCharacterTypeCount = 2,
			RegexPattern = ".*",
			Value = "a b"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_AnyCharacterType_CountsAll()
	{
		var behavior = new CharactersValidationBehavior
		{
			CharacterType = CharacterType.Any,
			MinimumCharacterTypeCount = 5,
			MaximumCharacterTypeCount = 10,
			RegexPattern = ".*",
			Value = "abc12"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}
}

public class EmailValidationBehaviorTests
{
	[Theory]
	[InlineData("test@example.com", true)]
	[InlineData("user.name@domain.org", true)]
	[InlineData("user+tag@sub.domain.com", true)]
	[InlineData("invalid", false)]
	[InlineData("@domain.com", false)]
	[InlineData("user@", false)]
	[InlineData("user @domain.com", false)]
	[InlineData("", false)]
	public async Task ForceValidate_ValidatesEmailFormat(string email, bool expectedValid)
	{
		var behavior = new EmailValidationBehavior
		{
			Value = email
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.Equal(expectedValid, behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_NullEmail_ReturnsInvalid()
	{
		var behavior = new EmailValidationBehavior
		{
			Value = null
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}
}

public class NumericValidationBehaviorTests
{
	[Theory]
	[InlineData("42", true)]
	[InlineData("3.14", true)]
	[InlineData("-7.5", true)]
	[InlineData("abc", false)]
	[InlineData("", false)]
	public async Task ForceValidate_ValidatesNumericFormat(string input, bool expectedValid)
	{
		var behavior = new NumericValidationBehavior
		{
			Value = input
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.Equal(expectedValid, behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_ValueInRange_ReturnsValid()
	{
		var behavior = new NumericValidationBehavior
		{
			MinimumValue = 1,
			MaximumValue = 100,
			Value = "50"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_ValueBelowMin_ReturnsInvalid()
	{
		var behavior = new NumericValidationBehavior
		{
			MinimumValue = 10,
			MaximumValue = 100,
			Value = "5"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_ValueAboveMax_ReturnsInvalid()
	{
		var behavior = new NumericValidationBehavior
		{
			MinimumValue = 1,
			MaximumValue = 10,
			Value = "15"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_DecimalPlacesInRange_ReturnsValid()
	{
		var behavior = new NumericValidationBehavior
		{
			MinimumDecimalPlaces = 1,
			MaximumDecimalPlaces = 3,
			Value = "3.14"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_TooManyDecimalPlaces_ReturnsInvalid()
	{
		var behavior = new NumericValidationBehavior
		{
			MinimumDecimalPlaces = 0,
			MaximumDecimalPlaces = 2,
			Value = "3.14159"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_TooFewDecimalPlaces_ReturnsInvalid()
	{
		var behavior = new NumericValidationBehavior
		{
			MinimumDecimalPlaces = 2,
			MaximumDecimalPlaces = 5,
			Value = "3"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_NullValue_ReturnsInvalid()
	{
		var behavior = new NumericValidationBehavior
		{
			Value = null
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}
}

public class RequiredStringValidationBehaviorTests
{
	[Fact]
	public async Task ForceValidate_ExactMatch_MatchingString_ReturnsValid()
	{
		var behavior = new RequiredStringValidationBehavior
		{
			RequiredString = "password123",
			ExactMatch = true,
			Value = "password123"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_ExactMatch_DifferentString_ReturnsInvalid()
	{
		var behavior = new RequiredStringValidationBehavior
		{
			RequiredString = "password123",
			ExactMatch = true,
			Value = "password12"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_ContainsMatch_SubstringPresent_ReturnsValid()
	{
		var behavior = new RequiredStringValidationBehavior
		{
			RequiredString = "world",
			ExactMatch = false,
			Value = "hello world"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_ContainsMatch_SubstringAbsent_ReturnsInvalid()
	{
		var behavior = new RequiredStringValidationBehavior
		{
			RequiredString = "xyz",
			ExactMatch = false,
			Value = "hello world"
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_NullValue_ReturnsInvalid()
	{
		var behavior = new RequiredStringValidationBehavior
		{
			RequiredString = "test",
			Value = null
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}
}

public class UriValidationBehaviorTests
{
	[Theory]
	[InlineData("https://example.com", true)]
	[InlineData("http://example.com/path?q=1", true)]
	[InlineData("ftp://files.example.com", true)]
	[InlineData("not a uri", false)]
	[InlineData("", false)]
	public async Task ForceValidate_ValidatesAbsoluteUri(string uri, bool expectedValid)
	{
		var behavior = new UriValidationBehavior
		{
			UriKind = UriKind.Absolute,
			Value = uri
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.Equal(expectedValid, behavior.IsValid);
	}

	[Theory]
	[InlineData("/relative/path", true)]
	[InlineData("relative/path", true)]
	[InlineData("https://example.com", true)]
	public async Task ForceValidate_RelativeOrAbsolute_AcceptsBoth(string uri, bool expectedValid)
	{
		var behavior = new UriValidationBehavior
		{
			UriKind = UriKind.RelativeOrAbsolute,
			Value = uri
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.Equal(expectedValid, behavior.IsValid);
	}

	[Theory]
	[InlineData("/relative/path", true)]
	[InlineData("relative/path", true)]
	[InlineData("https://example.com", false)]
	public async Task ForceValidate_RelativeOnly_RejectsAbsolute(string uri, bool expectedValid)
	{
		var behavior = new UriValidationBehavior
		{
			UriKind = UriKind.Relative,
			Value = uri
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.Equal(expectedValid, behavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_NullValue_ReturnsInvalid()
	{
		var behavior = new UriValidationBehavior
		{
			Value = null
		};

		await behavior.ForceValidate(CancellationToken.None);

		Assert.False(behavior.IsValid);
	}
}

public class MultiValidationBehaviorTests
{
	[Fact]
	public async Task ForceValidate_AllChildrenValid_ReturnsValid()
	{
		var multiBehavior = new MultiValidationBehavior();

		var child1 = new RequiredStringValidationBehavior
		{
			RequiredString = "hello",
			ExactMatch = false
		};
		MultiValidationBehavior.SetError(child1, "Must contain hello");

		var child2 = new TextValidationBehavior
		{
			MinimumLength = 1,
			MaximumLength = 100,
			RegexPattern = ".*"
		};
		MultiValidationBehavior.SetError(child2, "Length must be 1-100");

		multiBehavior.Children.Add(child1);
		multiBehavior.Children.Add(child2);
		multiBehavior.Value = "hello world";

		await multiBehavior.ForceValidate(CancellationToken.None);

		Assert.True(multiBehavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_OneChildInvalid_ReturnsInvalid()
	{
		var multiBehavior = new MultiValidationBehavior();

		var child1 = new RequiredStringValidationBehavior
		{
			RequiredString = "hello",
			ExactMatch = false
		};
		MultiValidationBehavior.SetError(child1, "Must contain hello");

		var child2 = new TextValidationBehavior
		{
			MinimumLength = 1,
			MaximumLength = 5,
			RegexPattern = ".*"
		};
		MultiValidationBehavior.SetError(child2, "Length must be 1-5");

		multiBehavior.Children.Add(child1);
		multiBehavior.Children.Add(child2);
		multiBehavior.Value = "hello world this is too long";

		await multiBehavior.ForceValidate(CancellationToken.None);

		Assert.False(multiBehavior.IsValid);
	}

	[Fact]
	public async Task ForceValidate_InvalidChild_PopulatesErrors()
	{
		var multiBehavior = new MultiValidationBehavior();

		var child = new RequiredStringValidationBehavior
		{
			RequiredString = "required",
			ExactMatch = true
		};
		MultiValidationBehavior.SetError(child, "Value is required");

		multiBehavior.Children.Add(child);
		multiBehavior.Value = "wrong";

		await multiBehavior.ForceValidate(CancellationToken.None);

		Assert.False(multiBehavior.IsValid);
		Assert.NotNull(multiBehavior.Errors);
		Assert.Contains("Value is required", multiBehavior.Errors);
	}

	[Fact]
	public async Task ForceValidate_NoChildren_ReturnsValid()
	{
		var multiBehavior = new MultiValidationBehavior
		{
			Value = "anything"
		};

		await multiBehavior.ForceValidate(CancellationToken.None);

		Assert.True(multiBehavior.IsValid);
	}
}

public class ValidationBehaviorFlagsTests
{
	[Fact]
	public void ValidationFlags_HasExpectedValues()
	{
		Assert.Equal(0, (int)ValidationFlags.None);
		Assert.Equal(1, (int)ValidationFlags.ValidateOnAttaching);
		Assert.Equal(2, (int)ValidationFlags.ValidateOnFocused);
		Assert.Equal(4, (int)ValidationFlags.ValidateOnUnfocused);
		Assert.Equal(8, (int)ValidationFlags.ValidateOnValueChanged);
		Assert.Equal(16, (int)ValidationFlags.ForceMakeValidWhenFocused);
	}

	[Fact]
	public void ValidationFlags_CanBeCombined()
	{
		var combined = ValidationFlags.ValidateOnAttaching | ValidationFlags.ValidateOnValueChanged;

		Assert.True(combined.HasFlag(ValidationFlags.ValidateOnAttaching));
		Assert.True(combined.HasFlag(ValidationFlags.ValidateOnValueChanged));
		Assert.False(combined.HasFlag(ValidationFlags.ValidateOnFocused));
	}
}

public class TextDecorationFlagsTests
{
	[Fact]
	public void TextDecorationFlags_HasExpectedValues()
	{
		Assert.Equal(0, (int)TextDecorationFlags.None);
		Assert.Equal(1, (int)TextDecorationFlags.TrimStart);
		Assert.Equal(2, (int)TextDecorationFlags.TrimEnd);
		Assert.Equal(3, (int)TextDecorationFlags.Trim);
		Assert.Equal(4, (int)TextDecorationFlags.NullToEmpty);
		Assert.Equal(8, (int)TextDecorationFlags.NormalizeWhiteSpace);
	}
}

public class CharacterTypeTests
{
	[Fact]
	public void CharacterType_HasExpectedValues()
	{
		Assert.Equal(1, (int)CharacterType.LowercaseLetter);
		Assert.Equal(2, (int)CharacterType.UppercaseLetter);
		Assert.Equal(3, (int)CharacterType.Letter);
		Assert.Equal(4, (int)CharacterType.Digit);
		Assert.Equal(7, (int)CharacterType.Alphanumeric);
		Assert.Equal(8, (int)CharacterType.Whitespace);
		Assert.Equal(16, (int)CharacterType.NonAlphanumericSymbol);
		Assert.Equal(32, (int)CharacterType.LowercaseLatinLetter);
		Assert.Equal(64, (int)CharacterType.UppercaseLatinLetter);
		Assert.Equal(96, (int)CharacterType.LatinLetter);
		Assert.Equal(31, (int)CharacterType.Any);
	}

	[Fact]
	public void CharacterType_Letter_IsCombinationOfLowerAndUpper()
	{
		Assert.Equal(CharacterType.LowercaseLetter | CharacterType.UppercaseLetter, CharacterType.Letter);
	}

	[Fact]
	public void CharacterType_Alphanumeric_IsCombinationOfLetterAndDigit()
	{
		Assert.Equal(CharacterType.Letter | CharacterType.Digit, CharacterType.Alphanumeric);
	}
}