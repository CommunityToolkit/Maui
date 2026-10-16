using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Handlers;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of the validation behaviors (ValidationBehavior and the behaviors built on it) on an Entry, with the settings of the
// Sample's validation pages: the ValidStyle or InvalidStyle that the behavior applies for its result must reach the platform view.
// The platform partials read the native text color.
[Category(TestCategory.ValidationBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class ValidationBehaviorTests : CommunityToolkitHandlerTestBase
{
	static readonly Color validTextColor = Colors.Green;
	static readonly Color invalidTextColor = Colors.Red;

	// EmailValidationBehaviorPage: DecorationFlags="Trim" validates the text without its leading and trailing white space.
	[Fact]
	public async Task TextIsValidatedWithoutLeadingAndTrailingWhiteSpaceWhenDecorationFlagsIsTrim()
	{
		SetupBuilder();

		var validStyle = CreateTextColorStyle(validTextColor);
		var invalidStyle = CreateTextColorStyle(invalidTextColor);
		var behavior = new EmailValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnValueChanged,
			DecorationFlags = TextDecorationFlags.Trim,
			ValidStyle = validStyle,
			InvalidStyle = invalidStyle
		};

		var entry = new Entry { WidthRequest = 300 };
		entry.Behaviors.Add(behavior);

		// In a window, as in an app: on Windows a TextBox that is in no window keeps its theme Foreground (CI run 38115049024).
		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();

			entry.Text = " user@example.com ";
			await AssertValidationResultAsync(entry, handler, behavior, isValid: true, validStyle, invalidStyle);

			// White space inside the text is not removed
			entry.Text = " user @example.com ";
			await AssertValidationResultAsync(entry, handler, behavior, isValid: false, validStyle, invalidStyle);
		});
	}

	// CharactersValidationBehaviorPage: Flags="ValidateOnValueChanged", and the rules are bound to other controls. Changing a rule there
	// takes the focus from the Entry (the Entry of a count does, and so does the Picker on iOS), and the new rule must still apply to its text.
	[Fact]
	public async Task TextIsValidatedAgainWhenARuleChangesAfterTheEntryLostTheFocus()
	{
		SetupBuilder();

		var validStyle = CreateTextColorStyle(validTextColor);
		var invalidStyle = CreateTextColorStyle(invalidTextColor);
		var behavior = new CharactersValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnValueChanged,
			CharacterType = CharacterType.Digit,
			MinimumCharacterTypeCount = 2,
			MaximumCharacterTypeCount = 20,
			ValidStyle = validStyle,
			InvalidStyle = invalidStyle
		};

		var entry = new Entry { WidthRequest = 300 };
		entry.Behaviors.Add(behavior);

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();

			await FocusAsync(entry);
			entry.Text = "12";
			await AssertValidationResultAsync(entry, handler, behavior, isValid: true, validStyle, invalidStyle);

			await UnfocusAsync(entry);

			// "12" has fewer than 5 digits
			behavior.MinimumCharacterTypeCount = 5;
			await AssertValidationResultAsync(entry, handler, behavior, isValid: false, validStyle, invalidStyle);

			behavior.MinimumCharacterTypeCount = 2;
			await AssertValidationResultAsync(entry, handler, behavior, isValid: true, validStyle, invalidStyle);

			// "12" has no letter
			behavior.CharacterType = CharacterType.Letter;
			await AssertValidationResultAsync(entry, handler, behavior, isValid: false, validStyle, invalidStyle);
		});
	}

	static async Task FocusAsync(Entry entry)
	{
		Assert.True(entry.Focus(), "The Entry did not accept the focus.");
		await AssertEventually(() => entry.IsFocused, timeout: 3000, message: "The Entry never got the focus.");
	}

	static async Task UnfocusAsync(Entry entry)
	{
		entry.Unfocus();
		await AssertEventually(() => !entry.IsFocused, timeout: 3000, message: "The Entry never lost the focus.");
	}

	static Style CreateTextColorStyle(Color textColor) => new(typeof(Entry))
	{
		Setters =
		{
			new Setter { Property = Entry.TextColorProperty, Value = textColor }
		}
	};

	// The behavior validates the text and applies the style before the property setter returns, so the wait only covers a platform
	// view that is updated later.
	static async Task AssertValidationResultAsync(Entry entry, IPlatformViewHandler handler, ValidationBehavior behavior, bool isValid, Style validStyle, Style invalidStyle)
	{
		var expectedTextColor = isValid ? validTextColor : invalidTextColor;

		await Wait(() => behavior.IsValid == isValid && IsSameColor(GetPlatformTextColor(handler), expectedTextColor), timeout: 3000);

		Assert.Equal(isValid, behavior.IsValid);
		Assert.Same(isValid ? validStyle : invalidStyle, entry.Style);

		var platformTextColor = GetPlatformTextColor(handler);
		Assert.True(IsSameColor(platformTextColor, expectedTextColor), $"The platform text color is {platformTextColor?.ToString() ?? "null"}, expected {expectedTextColor}.");
	}

	static bool IsSameColor(Color? actual, Color expected) =>
		actual is not null
		&& Math.Abs(actual.Red - expected.Red) < 0.01
		&& Math.Abs(actual.Green - expected.Green) < 0.01
		&& Math.Abs(actual.Blue - expected.Blue) < 0.01
		&& Math.Abs(actual.Alpha - expected.Alpha) < 0.01;
}