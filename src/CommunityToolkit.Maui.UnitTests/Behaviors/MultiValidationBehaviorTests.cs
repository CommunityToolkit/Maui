using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class MultiValidationBehaviorTests : BaseTest
{
	[Theory]
	[InlineData(CharacterType.Any, 1, 2, "A", "A", true)]
	[InlineData(CharacterType.Any, 0, int.MaxValue, "", "A", false)]
	[InlineData(CharacterType.LowercaseLetter, 1, int.MaxValue, "WWWWWaWWWW", "WWW", true)]
	[InlineData(CharacterType.UppercaseLetter, 1, int.MaxValue, "aaaaaaRRaaaa", "RRa", true)]
	[InlineData(CharacterType.Letter, 4, int.MaxValue, "aaaaaaRRaaaa", "RRa", true)]
	[InlineData(CharacterType.Digit, 1, int.MaxValue, "-1d", "1", true)]
	[InlineData(CharacterType.Alphanumeric, 2, int.MaxValue, "@-3r", "3", true, false)]
	[InlineData(CharacterType.NonAlphanumericSymbol, 10, int.MaxValue, "@-&^%!+()/", "+(", true)]
	[InlineData(CharacterType.LowercaseLatinLetter, 2, int.MaxValue, "HHHH a    r.", "HHHH a    r.", true)]
	[InlineData(CharacterType.UppercaseLatinLetter, 2, int.MaxValue, "aaaaaa....R.R.R.aaaa", "aaaaaa....R.R.R.aaaa", true)]
	[InlineData(CharacterType.LatinLetter, 5, int.MaxValue, "12345bBbBb", "bBb", true)]
	[InlineData(CharacterType.Whitespace, 0, int.MaxValue, ";lkjhgfd@+fasf", ";lkjhgfd@+fasf", true)]
	[InlineData(CharacterType.Any, 2, 2, "A", "A", false)]
	[InlineData(CharacterType.Any, 2, 2, "AaA", "A", false)]
	[InlineData(CharacterType.Any, 1, int.MaxValue, "", "", false)]
	[InlineData(CharacterType.Any, 1, int.MaxValue, null, "a", false)]
	[InlineData(CharacterType.LowercaseLetter, 1, int.MaxValue, "WWWWWW", "WWW", false)]
	[InlineData(CharacterType.UppercaseLetter, 1, int.MaxValue, "aaaaaa", "aaa", false)]
	[InlineData(CharacterType.Letter, 4, int.MaxValue, "wHo", "abc", false)]
	[InlineData(CharacterType.Digit, 1, int.MaxValue, "-d", "1", false)]
	[InlineData(CharacterType.Alphanumeric, 2, int.MaxValue, "@-3", "-3", false)]
	[InlineData(CharacterType.NonAlphanumericSymbol, 1, int.MaxValue, "WWWWWWWW", "WWW", false)]
	[InlineData(CharacterType.LowercaseLatinLetter, 1, int.MaxValue, "Кириллица", "aaa", false)]
	[InlineData(CharacterType.UppercaseLatinLetter, 1, int.MaxValue, "КИРИЛЛИЦА", "aaa", false)]
	[InlineData(CharacterType.LatinLetter, 1, int.MaxValue, "Это Кириллица!", "!", false)]
	[InlineData(CharacterType.Whitespace, 0, 0, "WWWWWW WWWWW", "WWW", false)]
	public async Task IsValid(CharacterType characterType, int minimumCharactersNumber, int maximumCharactersNumber, string value, string requiredString, bool expectedValue, bool exactMatch = false)
	{
		// Arrange
		var characterValidationBehavior = new CharactersValidationBehavior
		{
			CharacterType = characterType,
			MinimumCharacterTypeCount = minimumCharactersNumber,
			MaximumCharacterTypeCount = maximumCharactersNumber
		};
		var requiredStringValidationBehavior = new RequiredStringValidationBehavior
		{
			RequiredString = requiredString,
			ExactMatch = exactMatch
		};

		var multiBehavior = new MultiValidationBehavior();
		multiBehavior.Children.Add(characterValidationBehavior);
		multiBehavior.Children.Add(requiredStringValidationBehavior);

		var entry = new Entry
		{
			Text = value
		};
		entry.Behaviors.Add(multiBehavior);

		// Act
		await multiBehavior.ForceValidate();

		// Assert
		Assert.Equal(expectedValue, multiBehavior.IsValid);
	}
}