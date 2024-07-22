using CommunityToolkit.Maui.Behaviors;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class CharactersValidationBehaviorTests() : BaseBehaviorTest<CharactersValidationBehavior, VisualElement>(new CharactersValidationBehavior(), new View())
{
	[Theory]
	[InlineData(CharacterType.Any, 1, 2, "A", true)]
	[InlineData(CharacterType.Any, 0, int.MaxValue, "", true)]
	[InlineData(CharacterType.LowercaseLetter, 1, int.MaxValue, "WWWWWaWWWW", true)]
	[InlineData(CharacterType.UppercaseLetter, 1, int.MaxValue, "aaaaaaRRaaaa", true)]
	[InlineData(CharacterType.Letter, 4, int.MaxValue, "aaaaaaRRaaaa", true)]
	[InlineData(CharacterType.Digit, 1, int.MaxValue, "-1d", true)]
	[InlineData(CharacterType.Alphanumeric, 2, int.MaxValue, "@-3r", true)]
	[InlineData(CharacterType.NonAlphanumericSymbol, 10, int.MaxValue, "@-&^%!+()/", true)]
	[InlineData(CharacterType.LowercaseLatinLetter, 2, int.MaxValue, "HHHH a    r.", true)]
	[InlineData(CharacterType.UppercaseLatinLetter, 2, int.MaxValue, "aaaaaa....R.R.R.aaaa", true)]
	[InlineData(CharacterType.LatinLetter, 5, int.MaxValue, "12345bBbBb", true)]
	[InlineData(CharacterType.Whitespace, 0, int.MaxValue, ";lkjhgfd@+fasf", true)]
	[InlineData(CharacterType.Any, 2, 2, "A", false)]
	[InlineData(CharacterType.Any, 2, 2, "AaA", false)]
	[InlineData(CharacterType.Any, 1, int.MaxValue, "", false)]
	[InlineData(CharacterType.Any, 1, int.MaxValue, null, false)]
	[InlineData(CharacterType.LowercaseLetter, 1, int.MaxValue, "WWWWWW", false)]
	[InlineData(CharacterType.UppercaseLetter, 1, int.MaxValue, "aaaaaa", false)]
	[InlineData(CharacterType.Letter, 4, int.MaxValue, "wHo", false)]
	[InlineData(CharacterType.Digit, 1, int.MaxValue, "-d", false)]
	[InlineData(CharacterType.Alphanumeric, 2, int.MaxValue, "@-3", false)]
	[InlineData(CharacterType.NonAlphanumericSymbol, 1, int.MaxValue, "WWWWWWWW", false)]
	[InlineData(CharacterType.LowercaseLatinLetter, 1, int.MaxValue, "Кириллица", false)]
	[InlineData(CharacterType.UppercaseLatinLetter, 1, int.MaxValue, "КИРИЛЛИЦА", false)]
	[InlineData(CharacterType.LatinLetter, 1, int.MaxValue, "Это Кириллица!", false)]
	[InlineData(CharacterType.Whitespace, 0, 0, "WWWWWW WWWWW", false)]
	public async Task IsValid(CharacterType characterType, int minimumCharactersNumber, int maximumCharactersNumber, string? value, bool expectedValue)
	{
		// Arrange
		var behavior = new CharactersValidationBehavior
		{
			CharacterType = characterType,
			MinimumCharacterTypeCount = minimumCharactersNumber,
			MaximumCharacterTypeCount = maximumCharactersNumber
		};

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
	public async Task CancellationTokenExpired()
	{
		// Arrange
		var behavior = new CharactersValidationBehavior();
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
	public async Task CancellationTokenCanceled()
	{
		// Arrange
		var behavior = new CharactersValidationBehavior();
		var cts = new CancellationTokenSource();

		var entry = new Entry
		{
			Text = "Hello"
		};
		entry.Behaviors.Add(behavior);

		// Act

		// Ensure CancellationToken expires
		await Task.Delay(100, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
		{
			await cts.CancelAsync();
			await behavior.ForceValidate(cts.Token);
		});
	}

	[Fact]
	public void EnsureObjectDisposedExceptionThrownWhenDisposedBehaviorAttachedToVisualElement()
	{
		var behavior = new CharactersValidationBehavior();

		Assert.Throws<ObjectDisposedException>(() =>
		{
			// Use AsyncContext to catch async void exception
			AsyncContext.Run(() =>
			{
				behavior.Dispose();
				var element = new VisualElement()
				{
					Behaviors =
					{
						behavior
					}
				};
			});
		});
	}
}