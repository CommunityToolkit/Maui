using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class TextValidationBehaviorTests() : BaseBehaviorTest<TextValidationBehavior, VisualElement>(new CharactersValidationBehavior(), new MockView())
{
	[Theory]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 5, 25, TextDecorationFlags.None, "Microsoft", true)]
	[InlineData("mi.....ft", RegexOptions.None, 5, 25, TextDecorationFlags.Trim, "minecraft    ", true)]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 5, 25, TextDecorationFlags.None, "microservice", false)]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 5, 6, TextDecorationFlags.None, "Microsoft", false)]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 10, 11, TextDecorationFlags.None, "Microsoft", false)]
	[InlineData("^[0-9]*$", RegexOptions.None, 0, int.MaxValue, TextDecorationFlags.None, " 12 ", false)]
	[InlineData("^[0-9]*$", RegexOptions.None, 0, int.MaxValue, TextDecorationFlags.Trim, " 12 ", true)]
	[InlineData("^[0-9]*$", RegexOptions.None, 0, int.MaxValue, TextDecorationFlags.TrimStart, " 12", true)]
	[InlineData("^[0-9]*$", RegexOptions.None, 0, int.MaxValue, TextDecorationFlags.TrimStart, "12 ", false)]
	[InlineData("^[0-9]*$", RegexOptions.None, 0, int.MaxValue, TextDecorationFlags.TrimEnd, "12 ", true)]
	[InlineData("^[0-9]*$", RegexOptions.None, 0, int.MaxValue, TextDecorationFlags.TrimEnd, " 12", false)]
	[InlineData(".*", RegexOptions.None, 2, 2, TextDecorationFlags.None, " 12 ", false)]
	[InlineData(".*", RegexOptions.None, 2, 2, TextDecorationFlags.Trim, " 12 ", true)]
	[InlineData("^Hello World$", RegexOptions.None, 0, int.MaxValue, TextDecorationFlags.None, "Hello    World", false)]
	[InlineData("^Hello World$", RegexOptions.None, 0, int.MaxValue, TextDecorationFlags.NormalizeWhiteSpace, "Hello    World", true)]
	public async Task IsValid(string pattern, RegexOptions options, int minLength, int maxLength, TextDecorationFlags flags, string value, bool expectedValue)
	{
		// Arrange
		var behavior = new TextValidationBehavior
		{
			RegexPattern = pattern,
			RegexOptions = options,
			MinimumLength = minLength,
			MaximumLength = maxLength,
			DecorationFlags = flags
		};

		var entry = new Entry
		{
			Text = value
		};
		RunOnUIThread(() => entry.Behaviors.Add(behavior));

		// Act
		await behavior.ForceValidate(TestContext.Current.CancellationToken);

		// Assert
		Assert.Equal(expectedValue, behavior.IsValid);
	}

	[Theory]
	[InlineData(TextDecorationFlags.None, false)]
	[InlineData(TextDecorationFlags.NullToEmpty, true)]
	public async Task IsValid_NullText_IsValidatedAsAnEmptyStringWhenDecorationFlagsIncludesNullToEmpty(TextDecorationFlags flags, bool expectedValue)
	{
		// The default RegexPattern ("") and MinimumLength (0) accept an empty string
		var behavior = new TextValidationBehavior
		{
			DecorationFlags = flags
		};

		var entry = new Entry
		{
			Text = null
		};
		entry.Behaviors.Add(behavior);

		await behavior.ForceValidate(TestContext.Current.CancellationToken);

		Assert.Equal(expectedValue, behavior.IsValid);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenExpired()
	{
		// Arrange
		var behavior = new TextValidationBehavior();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var entry = new Entry
		{
			Text = "Hello"
		};
		RunOnUIThread(() => entry.Behaviors.Add(behavior));

		// Act
		await WaitForCancellationAsync(cts.Token, TestContext.Current.CancellationToken);

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () => await behavior.ForceValidate(cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenCanceled()
	{
		// Arrange
		var behavior = new TextValidationBehavior();
		var cts = new CancellationTokenSource();

		var entry = new Entry
		{
			Text = "Hello"
		};
		RunOnUIThread(() => entry.Behaviors.Add(behavior));

		// Act

		// Ensure CancellationToken expires
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
		{
			await cts.CancelAsync();
			await behavior.ForceValidate(cts.Token);
		});
	}

	[Fact]
	public void IsValid_ShouldRevalidate_WhenRegexPatternChangesAfterTheViewLostFocus()
	{
		var behavior = new TextValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnValueChanged,
			RegexPattern = "^[0-9]*$"
		};

		var entry = new Entry();
		entry.Behaviors.Add(behavior);

		((IView)entry).IsFocused = true;
		entry.Text = "abc";
		((IView)entry).IsFocused = false;

		Assert.False(behavior.IsValid);

		behavior.RegexPattern = "^[a-z]*$";

		Assert.True(behavior.IsValid);
	}

	// Mirrors the settings of the Sample's TextValidationBehaviorPage.
	[Fact]
	public void IsValid_ShouldRevalidate_WhenDecorationFlagsChangesAfterTheViewLostFocus()
	{
		var behavior = new TextValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnValueChanged,
			RegexPattern = "^[0-9]*$"
		};

		var entry = new Entry();
		entry.Behaviors.Add(behavior);

		((IView)entry).IsFocused = true;
		entry.Text = "12 ";
		((IView)entry).IsFocused = false;

		Assert.False(behavior.IsValid);

		behavior.DecorationFlags = TextDecorationFlags.Trim;

		Assert.True(behavior.IsValid);
	}

	[Fact]
	public void IsValid_ShouldStayValid_WhenARuleChangesWhileTheViewIsFocusedAndFlagsIncludesForceMakeValidWhenFocused()
	{
		var behavior = new TextValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnUnfocused | ValidationFlags.ForceMakeValidWhenFocused,
			MinimumLength = 1
		};

		var entry = new Entry();
		entry.Behaviors.Add(behavior);

		((IView)entry).IsFocused = true;
		entry.Text = "abc";
		((IView)entry).IsFocused = false;
		((IView)entry).IsFocused = true;

		behavior.MinimumLength = 5;

		Assert.True(behavior.IsValid);

		((IView)entry).IsFocused = false;

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public void IsValid_ShouldNotValidate_WhenARuleChangesBeforeTheFirstValidationOfAnotherView()
	{
		var behavior = new TextValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnUnfocused
		};

		var firstEntry = new Entry();
		firstEntry.Behaviors.Add(behavior);

		((IView)firstEntry).IsFocused = true;
		firstEntry.Text = "abc";
		((IView)firstEntry).IsFocused = false;
		firstEntry.Behaviors.Remove(behavior);

		var secondEntry = new Entry();
		RunOnUIThread(() => secondEntry.Behaviors.Add(behavior));

		behavior.MinimumLength = 1;

		Assert.True(behavior.IsValid);

		((IView)secondEntry).IsFocused = true;
		((IView)secondEntry).IsFocused = false;

		Assert.False(behavior.IsValid);
	}

	[Fact]
	public void VerifyDefaults()
	{
		// Arrange
		var textValidationBehavior = new TextValidationBehavior();

		// Act Assert
		Assert.Equal(TextValidationBehaviorDefaults.DecorationFlags, textValidationBehavior.DecorationFlags);
		Assert.Equal(TextValidationBehaviorDefaults.MaximumLength, textValidationBehavior.MaximumLength);
		Assert.Equal(TextValidationBehaviorDefaults.MinimumLength, textValidationBehavior.MinimumLength);
		Assert.Equal(TextValidationBehaviorDefaults.RegexOptions, textValidationBehavior.RegexOptions);
		Assert.Equal(TextValidationBehaviorDefaults.RegexPattern, textValidationBehavior.RegexPattern);
	}
}