using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>Flags to indicate what treatment <see cref="ValidationBehavior.Value"/> should receive prior to validation with <see cref="ValidationBehavior"/> or subclasses. This can be used to trim or ignore whitespace for instance. This value might be ignored by a behavior if <see cref="ValidationBehavior.Value"/> isn't of type <see cref="string"/>.</summary>
[Flags]
public enum TextDecorationFlags
{
	/// <summary>No text decoration will be applied.</summary>
	None = 0,
	/// <summary><see cref="string.TrimStart()"/> is applied on the value prior to validation.</summary>
	TrimStart = 1,
	/// <summary><see cref="string.TrimEnd()"/> is applied on the value prior to validation.</summary>
	TrimEnd = 2,
	/// <summary><see cref="string.Trim()"/> is applied on the value prior to validation.</summary>
	Trim = TrimStart | TrimEnd,
	/// <summary>If <see cref="ValidationBehavior.Value"/> is null, replace the value with <see cref="string.Empty"/></summary>
	NullToEmpty = 4,
	/// <summary>Excessive white space is removed from <see cref="ValidationBehavior.Value"/> prior to validation. I.e. I.e. "Hello    World" will become "Hello World". This applies to whitespace found anywhere.</summary>
	NormalizeWhiteSpace = 8
}

/// <summary>
/// The <see cref="TextValidationBehavior"/> is a behavior that allows the user to validate a given text depending on specified parameters. By adding this behavior to an <see cref="InputView"/> inherited control (i.e. <see cref="Entry"/>) it can be styled differently depending on whether a valid or an invalid text value is provided. It offers various built-in checks such as checking for a certain length or whether or not the input value matches a specific regular expression. Additional properties handling validation are inherited from <see cref="ValidationBehavior"/>.
/// </summary>
public class TextValidationBehavior : ValidationBehavior<string>
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="MinimumLength"/> property.
	/// </summary>
	public static readonly BindableProperty MinimumLengthProperty =
		BindableProperty.Create(nameof(MinimumLength), typeof(int), typeof(TextValidationBehavior), 0, propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="MaximumLength"/> property.
	/// </summary>
	public static readonly BindableProperty MaximumLengthProperty =
		BindableProperty.Create(nameof(MaximumLength), typeof(int), typeof(TextValidationBehavior), int.MaxValue, propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="DecorationFlags"/> property.
	/// </summary>
	public static readonly BindableProperty DecorationFlagsProperty =
		BindableProperty.Create(nameof(DecorationFlags), typeof(TextDecorationFlags), typeof(TextValidationBehavior), TextDecorationFlags.None, propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="RegexPattern"/> property.
	/// </summary>
	public static readonly BindableProperty RegexPatternProperty =
		BindableProperty.Create(nameof(RegexPattern), typeof(string), typeof(TextValidationBehavior), defaultValueCreator: GetDefaultRegexPattern, propertyChanged: OnRegexPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="RegexOptions"/> property.
	/// </summary>
	public static readonly BindableProperty RegexOptionsProperty =
		BindableProperty.Create(nameof(RegexOptions), typeof(RegexOptions), typeof(TextValidationBehavior), defaultValueCreator: GetDefaultRegexOptions, propertyChanged: OnRegexPropertyChanged);

	Regex? regex;

	/// <summary>
	/// Constructor of this behavior.
	/// </summary>
	public TextValidationBehavior() => OnRegexPropertyChanged(RegexPattern, RegexOptions);

	/// <summary>
	/// Default regex pattern
	/// </summary>
	protected virtual string DefaultRegexPattern { get; } = string.Empty;

	/// <summary>
	/// Default regex options
	/// </summary>
	protected virtual RegexOptions DefaultRegexOptions { get; } = RegexOptions.None;

	/// <summary>
	/// The minimum length of the value that will be allowed. This is a bindable property.
	/// </summary>
	public int MinimumLength
	{
		get => (int)GetValue(MinimumLengthProperty);
		set => SetValue(MinimumLengthProperty, value);
	}

	/// <summary>
	/// The maximum length of the value that will be allowed. This is a bindable property.
	/// </summary>
	public int MaximumLength
	{
		get => (int)GetValue(MaximumLengthProperty);
		set => SetValue(MaximumLengthProperty, value);
	}

	/// <summary>
	/// Provides enumerated value to use to set how to handle white spaces. This is a bindable property.
	/// </summary>
	public TextDecorationFlags DecorationFlags
	{
		get => (TextDecorationFlags)GetValue(DecorationFlagsProperty);
		set => SetValue(DecorationFlagsProperty, value);
	}

	/// <summary>
	/// The regular expression pattern which the value will have to match before it will be allowed. This is a bindable property.
	/// </summary>
	public string? RegexPattern
	{
		get => (string?)GetValue(RegexPatternProperty);
		set => SetValue(RegexPatternProperty, value);
	}

	/// <summary>
	/// Provides enumerated values to use to set regular expression options. This is a bindable property.
	/// </summary>
	public RegexOptions RegexOptions
	{
		get => (RegexOptions)GetValue(RegexOptionsProperty);
		set => SetValue(RegexOptionsProperty, value);
	}

	/// <inheritdoc/>
	protected override string? Decorate(string? value)
	{
		var stringValue = base.Decorate(value)?.ToString();
		var flags = DecorationFlags;

		if (flags.HasFlag(TextDecorationFlags.NullToEmpty))
		{
			stringValue ??= string.Empty;
		}

		if (stringValue is null)
		{
			return null;
		}

		if (flags.HasFlag(TextDecorationFlags.TrimStart))
		{
			stringValue = stringValue.TrimStart();
		}

		if (flags.HasFlag(TextDecorationFlags.TrimEnd))
		{
			stringValue = stringValue.TrimEnd();
		}

		if (flags.HasFlag(TextDecorationFlags.NormalizeWhiteSpace))
		{
			stringValue = NormalizeWhiteSpace(stringValue);
		}

		return stringValue;
	}

	/// <inheritdoc/>
	protected override ValueTask<bool> ValidateAsync([NotNullWhen(true)] string? value, CancellationToken token)
	{
		return new ValueTask<bool>(
			value != null &&
			value.Length >= MinimumLength &&
			value.Length <= MaximumLength &&
			(regex?.IsMatch(value) ?? false));
	}

	static void OnRegexPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var textValidationBehavior = ((TextValidationBehavior)bindable);
		textValidationBehavior.OnRegexPropertyChanged(textValidationBehavior.RegexPattern, textValidationBehavior.RegexOptions);
		OnValidationPropertyChanged(bindable, oldValue, newValue);
	}

	static string GetDefaultRegexPattern(BindableObject bindable)
		=> ((TextValidationBehavior)bindable).DefaultRegexPattern;

	static object GetDefaultRegexOptions(BindableObject bindable)
		=> ((TextValidationBehavior)bindable).DefaultRegexOptions;

	// This method trims down multiple consecutive whitespaces
	// back to one whitespace.
	// I.e. "Hello    World" will become "Hello World"
	static string NormalizeWhiteSpace(string value)
	{
		var builder = new StringBuilder();
		var isSpace = false;

		foreach (var ch in value)
		{
			var wasSpace = isSpace;

			isSpace = char.IsWhiteSpace(ch);

			if (wasSpace && isSpace)
			{
				continue;
			}

			builder.Append(ch);
		}
		return builder.ToString();
	}

	void OnRegexPropertyChanged(string? regexPattern, RegexOptions regexOptions) => regex = regexPattern switch
	{
		null => null,
		_ => new Regex(regexPattern, regexOptions)
	};
}