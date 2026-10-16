using System.Collections.Concurrent;
using System.ComponentModel;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class MultiValidationBehaviorTests() : BaseBehaviorTest<MultiValidationBehavior, VisualElement>(new MultiValidationBehavior(), new MockView())
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
	public async Task IsValid(CharacterType characterType, int minimumCharactersNumber, int maximumCharactersNumber, string? value, string requiredString, bool expectedValue, bool exactMatch = false)
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
		RunOnUIThread(() => entry.Behaviors.Add(multiBehavior));

		// Act
		await multiBehavior.ForceValidate(TestContext.Current.CancellationToken);

		// Assert
		Assert.Equal(expectedValue, multiBehavior.IsValid);
	}

	[Theory]
	[InlineData(TextDecorationFlags.None, false)]
	[InlineData(TextDecorationFlags.Trim, true)]
	public async Task IsValid_ChildDecorationFlagsAreAppliedBeforeValidation(TextDecorationFlags flags, bool expectedValue)
	{
		var textValidationBehavior = new TextValidationBehavior
		{
			RegexPattern = "^[0-9]+$",
			DecorationFlags = flags
		};

		var multiBehavior = new MultiValidationBehavior();
		multiBehavior.Children.Add(textValidationBehavior);

		var entry = new Entry
		{
			Text = " 12 "
		};
		RunOnUIThread(() => entry.Behaviors.Add(multiBehavior));

		await multiBehavior.ForceValidate(TestContext.Current.CancellationToken);

		Assert.Equal(expectedValue, textValidationBehavior.IsValid);
		Assert.Equal(expectedValue, multiBehavior.IsValid);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ForceValidateCancellationTokenExpired()
	{
		// Arrange
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var characterValidationBehavior = new CharactersValidationBehavior();
		var requiredStringValidationBehavior = new RequiredStringValidationBehavior();

		var multiBehavior = new MultiValidationBehavior();
		multiBehavior.Children.Add(characterValidationBehavior);
		multiBehavior.Children.Add(requiredStringValidationBehavior);

		var entry = new Entry
		{
			Text = "Hello"
		};
		RunOnUIThread(() => entry.Behaviors.Add(multiBehavior));

		// Act

		// Ensure CancellationToken expires 
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () => await multiBehavior.ForceValidate(cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ForceValidateCancellationTokenCanceled()
	{
		// Arrange
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

		var characterValidationBehavior = new CharactersValidationBehavior();
		var requiredStringValidationBehavior = new RequiredStringValidationBehavior();

		var multiBehavior = new MultiValidationBehavior();
		multiBehavior.Children.Add(characterValidationBehavior);
		multiBehavior.Children.Add(requiredStringValidationBehavior);

		var entry = new Entry
		{
			Text = "Hello"
		};
		RunOnUIThread(() => entry.Behaviors.Add(multiBehavior));

		// Act

		// Ensure CancellationToken expires 
		await cts.CancelAsync();

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () => await multiBehavior.ForceValidate(cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void Errors_AreSetOnTheUIThread_WhenAChildValidatesAsynchronously()
	{
		var child = new AsynchronousValidationBehavior();
		MultiValidationBehavior.SetError(child, "Invalid");

		var multiBehavior = new MultiValidationBehavior();
		multiBehavior.Children.Add(child);

		var entry = new Entry
		{
			Text = "Hello"
		};
		var propertiesChangedOffTheUIThread = new ConcurrentQueue<string>();

		RunOnUIThread(async () =>
		{
			var uiThreadId = Environment.CurrentManagedThreadId;

			void RecordPropertyChangesOffTheUIThread(object? sender, PropertyChangedEventArgs e)
			{
				if (Environment.CurrentManagedThreadId != uiThreadId)
				{
					propertiesChangedOffTheUIThread.Enqueue($"{sender?.GetType().Name}.{e.PropertyName}");
				}
			}

			multiBehavior.PropertyChanged += RecordPropertyChangesOffTheUIThread;
			child.PropertyChanged += RecordPropertyChangesOffTheUIThread;
			entry.Behaviors.Add(multiBehavior);

			await multiBehavior.ForceValidate(TestContext.Current.CancellationToken);
		});

		Assert.Empty(propertiesChangedOffTheUIThread);
		Assert.NotNull(multiBehavior.Errors);
		Assert.Equal("Invalid", Assert.Single(multiBehavior.Errors));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void IsRunning_IsFalse_WhenForceValidateIsCancelledMidValidation()
	{
		var child = new MockValidationBehavior
		{
			SimulateValidationDelay = true
		};

		var multiBehavior = new MultiValidationBehavior();
		multiBehavior.Children.Add(child);

		var entry = new Entry
		{
			Text = "Hello"
		};
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

		RunOnUIThread(async () =>
		{
			entry.Behaviors.Add(multiBehavior);

			var forceValidateTask = multiBehavior.ForceValidate(cts.Token);
			Assert.True(multiBehavior.IsRunning);
			Assert.True(child.IsRunning);

			await cts.CancelAsync();
			await forceValidateTask;

			Assert.False(multiBehavior.IsRunning);
			Assert.False(child.IsRunning);
		});
	}

	[Fact]
	public void IsRunning_IsFalse_WhenTheViewGetsFocusedWhileAChildValidates()
	{
		var child = new MockValidationBehavior
		{
			SimulateValidationDelay = true
		};

		var multiBehavior = new MultiValidationBehavior();
		multiBehavior.Children.Add(child);

		var entry = new Entry();

		RunOnUIThread(() =>
		{
			entry.Behaviors.Add(multiBehavior);
			((IView)entry).IsFocused = true;
			((IView)entry).IsFocused = false;
			Assert.True(child.IsRunning);

			((IView)entry).IsFocused = true;
		});

		Assert.False(multiBehavior.IsRunning);
		Assert.False(child.IsRunning);
	}

	[Fact]
	public void VerifyDefaults()
	{
		// Arrange
		var multiValidationBehavior = new MultiValidationBehavior();

		// Act // Assert
		Assert.Equal(MultiValidationBehaviorDefaults.Errors, multiValidationBehavior.Errors);
		Assert.Equal(MultiValidationBehaviorDefaults.Error, MultiValidationBehavior.GetError(multiValidationBehavior));
	}

	sealed class AsynchronousValidationBehavior : ValidationBehavior<string>
	{
		protected override async ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
		{
			await Task.Yield();
			return false;
		}
	}
}