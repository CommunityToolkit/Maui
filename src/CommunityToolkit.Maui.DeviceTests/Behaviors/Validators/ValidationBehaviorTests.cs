using System.Globalization;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Handlers;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.ValidationBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class ValidationBehaviorTests : CommunityToolkitHandlerTestBase
{
	static readonly Color validTextColor = Colors.Green;
	static readonly Color invalidTextColor = Colors.Red;

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

			entry.Text = " user @example.com ";
			await AssertValidationResultAsync(entry, handler, behavior, isValid: false, validStyle, invalidStyle);
		});
	}

	// CharactersValidationBehaviorPage binds the rules to other controls, and changing one takes the focus from the Entry (the Entry of a
	// count does, and so does the Picker on iOS).
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

			behavior.MinimumCharacterTypeCount = 5;
			await AssertValidationResultAsync(entry, handler, behavior, isValid: false, validStyle, invalidStyle);

			behavior.MinimumCharacterTypeCount = 2;
			await AssertValidationResultAsync(entry, handler, behavior, isValid: true, validStyle, invalidStyle);

			behavior.CharacterType = CharacterType.Letter;
			await AssertValidationResultAsync(entry, handler, behavior, isValid: false, validStyle, invalidStyle);
		});
	}

	// Without DynamicallyAccessedMembers(All) on the validators, the trimmer may remove members that only a run reveals as missing.
	[Theory]
	[InlineData(ValidatorKind.Text, "abcd", "a")]
	[InlineData(ValidatorKind.Numeric, "5", "50")]
	[InlineData(ValidatorKind.RequiredString, "yes", "no")]
	[InlineData(ValidatorKind.Uri, "https://example.com", "not a uri")]
	[InlineData(ValidatorKind.Multi, "user@example.com", "ab")]
	public async Task EveryValidatorFollowsTheTextOfAnEntryAndFlipsIsValid(ValidatorKind kind, string validText, string invalidText)
	{
		SetupBuilder();

		var behavior = CreateValidator(kind);
		var entry = new Entry { WidthRequest = 300 };
		entry.Behaviors.Add(behavior);

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			entry.Text = invalidText;
			await Wait(() => !behavior.IsValid, timeout: 3000);
			Assert.False(behavior.IsValid);

			entry.Text = validText;
			await Wait(() => behavior.IsValid, timeout: 3000);
			Assert.True(behavior.IsValid);
		});
	}

	[Fact]
	public async Task MultiValidationBehaviorSetsTheErrorOfEachFailingChild()
	{
		SetupBuilder();

		const string minimumLengthError = "too short";
		const string emailError = "not an email";
		var textBehavior = new TextValidationBehavior { Flags = ValidationFlags.ValidateOnValueChanged, MinimumLength = 3 };
		var emailBehavior = new EmailValidationBehavior { Flags = ValidationFlags.ValidateOnValueChanged };
		var multiBehavior = new MultiValidationBehavior { Flags = ValidationFlags.ValidateOnValueChanged, Children = { textBehavior, emailBehavior } };
		MultiValidationBehavior.SetError(textBehavior, minimumLengthError);
		MultiValidationBehavior.SetError(emailBehavior, emailError);

		var entry = new Entry { WidthRequest = 300 };
		entry.Behaviors.Add(multiBehavior);

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			entry.Text = "ab";
			await Wait(() => multiBehavior.Errors?.Count is 2, timeout: 3000);
			Assert.NotNull(multiBehavior.Errors);
			Assert.Equal(new object?[] { minimumLengthError, emailError }, multiBehavior.Errors);

			entry.Text = "abcd";
			await Wait(() => multiBehavior.Errors?.Count is 1, timeout: 3000);
			Assert.Equal(new object?[] { emailError }, multiBehavior.Errors);

			entry.Text = "user@example.com";
			await Wait(() => multiBehavior.Errors is null, timeout: 3000);
			Assert.True(multiBehavior.IsValid);
		});
	}

	[Theory]
	[InlineData("", "3.5")]
	[InlineData("tr-TR", "3,5")]
	[InlineData("az-Latn-AZ", "3,5")]
	public async Task NumericValidationBehaviorUsesTheCurrentCultureDecimalSeparator(string cultureName, string validText)
	{
		SetupBuilder();

		var behavior = new NumericValidationBehavior { Flags = ValidationFlags.ValidateOnValueChanged, MinimumValue = 0, MaximumValue = 10, MaximumDecimalPlaces = 1 };
		var entry = new Entry { WidthRequest = 300 };
		entry.Behaviors.Add(behavior);

		await AttachAndRun(entry, async _ =>
		{
			var originalCulture = CultureInfo.CurrentCulture;
			var originalUICulture = CultureInfo.CurrentUICulture;
			try
			{
				var culture = CultureInfo.GetCultureInfo(cultureName);
				CultureInfo.CurrentCulture = culture;
				CultureInfo.CurrentUICulture = culture;
				await entry.OnLoadedAsync();

				entry.Text = "x";
				await Wait(() => !behavior.IsValid, timeout: 3000);

				entry.Text = validText;
				await Wait(() => behavior.IsValid, timeout: 3000);
				Assert.True(behavior.IsValid);
			}
			finally
			{
				CultureInfo.CurrentCulture = originalCulture;
				CultureInfo.CurrentUICulture = originalUICulture;
			}
		});
	}

	static ValidationBehavior CreateValidator(ValidatorKind kind) => kind switch
	{
		ValidatorKind.Text => new TextValidationBehavior { Flags = ValidationFlags.ValidateOnValueChanged, MinimumLength = 3 },
		ValidatorKind.Numeric => new NumericValidationBehavior { Flags = ValidationFlags.ValidateOnValueChanged, MinimumValue = 1, MaximumValue = 10 },
		ValidatorKind.RequiredString => new RequiredStringValidationBehavior { Flags = ValidationFlags.ValidateOnValueChanged, RequiredString = "yes" },
		ValidatorKind.Uri => new UriValidationBehavior { Flags = ValidationFlags.ValidateOnValueChanged, UriKind = UriKind.Absolute },
		ValidatorKind.Multi => new MultiValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnValueChanged,
			Children =
			{
				new TextValidationBehavior { Flags = ValidationFlags.ValidateOnValueChanged, MinimumLength = 3 },
				new EmailValidationBehavior { Flags = ValidationFlags.ValidateOnValueChanged }
			}
		},
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
	};

	public enum ValidatorKind
	{
		Text,
		Numeric,
		RequiredString,
		Uri,
		Multi
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