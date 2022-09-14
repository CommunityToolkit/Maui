namespace CommunityToolkit.Maui.Behaviors;

/// <summary>The allowed character types used to determine if a value is valid in the <see cref="CharactersValidationBehavior"/>. Since this is a flag, multiple flags cane be combined.</summary>
[Flags]
public enum CharacterType
{
	/// <summary>Lowercase characters are allowed.</summary>
	LowercaseLetter = 1,

	/// <summary>Uppercase characters are allowed.</summary>
	UppercaseLetter = 2,

	/// <summary>Either lowercase characters or uppercase characters are allowed.</summary>
	Letter = LowercaseLetter | UppercaseLetter,

	/// <summary>Digits are allowed.</summary>
	Digit = 4,

	/// <summary>Characters and digits are allowed.</summary>
	Alphanumeric = Letter | Digit,

	/// <summary>Whitespace is allowed.</summary>
	Whitespace = 8,

	/// <summary>Non-alphanumeric symbols are allowed.</summary>
	NonAlphanumericSymbol = 16,

	/// <summary>Lowercase latin characters are allowed.</summary>
	LowercaseLatinLetter = 32,

	/// <summary>Uppercase latin characters are allowed.</summary>
	UppercaseLatinLetter = 64,

	/// <summary>Either latin lowercase characters or latin uppercase characters are allowed.</summary>
	LatinLetter = LowercaseLatinLetter | UppercaseLatinLetter,

	/// <summary>Any type of character or digit either lowercase or uppercase, latin or non-latin is allowed.</summary>
	Any = Alphanumeric | NonAlphanumericSymbol | Whitespace
}

/// <summary>
/// The <see cref="CharactersValidationBehavior"/> is a behavior that allows the user to validate text input depending on specified parameters.For example, an <see cref="Entry"/> control can be styled differently depending on whether a valid or an invalid text value is provided. This behavior includes built-in checks such as checking for a certain number of digits or alphanumeric characters. Additional properties handling validation are inherited from <see cref="ValidationBehavior"/>.
/// </summary>
public class CharactersValidationBehavior : TextValidationBehavior
{
	List<Predicate<char>> characterPredicates = Enumerable.Empty<Predicate<char>>().ToList();

	/// <summary>
	/// Backing BindableProperty for the <see cref="CharacterType"/> property.
	/// </summary>
	public static readonly BindableProperty CharacterTypeProperty =
		BindableProperty.Create(nameof(CharacterType), typeof(CharacterType), typeof(CharactersValidationBehavior), CharacterType.Any, propertyChanged: OnCharacterTypePropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="MinimumCharacterTypeCount"/> property.
	/// </summary>
	public static readonly BindableProperty MinimumCharacterTypeCountProperty =
		BindableProperty.Create(nameof(MinimumCharacterTypeCount), typeof(int), typeof(CharactersValidationBehavior), 0, propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="MaximumCharacterTypeCount"/> property.
	/// </summary>
	public static readonly BindableProperty MaximumCharacterTypeCountProperty =
		BindableProperty.Create(nameof(MaximumCharacterTypeCount), typeof(int), typeof(CharactersValidationBehavior), int.MaxValue, propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Constructor for this behavior
	/// </summary>
	public CharactersValidationBehavior() => OnCharacterTypePropertyChanged();

	/// <summary>
	/// Provides an enumerated value to use to set how to handle comparisons. This is a bindable property.
	/// </summary>
	public CharacterType CharacterType
	{
		get => (CharacterType)GetValue(CharacterTypeProperty);
		set => SetValue(CharacterTypeProperty, value);
	}

	/// <summary>
	/// The minimum number of <see cref="CharacterType"/> required. This is a bindable property.
	/// </summary>
	public int MinimumCharacterTypeCount
	{
		get => (int)GetValue(MinimumCharacterTypeCountProperty);
		set => SetValue(MinimumCharacterTypeCountProperty, value);
	}

	/// <summary>
	/// The maximum number of <see cref="CharacterType"/> allowed. This is a bindable property.
	/// </summary>
	public int MaximumCharacterTypeCount
	{
		get => (int)GetValue(MaximumCharacterTypeCountProperty);
		set => SetValue(MaximumCharacterTypeCountProperty, value);
	}

	/// <inheritdoc/>
	protected override async ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
		=> await base.ValidateAsync(value, token).ConfigureAwait(false) && Validate(value);

	static void OnCharacterTypePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((CharactersValidationBehavior)bindable).OnCharacterTypePropertyChanged();
		OnValidationPropertyChanged(bindable, oldValue, newValue);
	}

	static IEnumerable<Predicate<char>> GetCharacterPredicates(CharacterType characterType)
	{
		if (characterType.HasFlag(CharacterType.LowercaseLetter))
		{
			yield return char.IsLower;
		}

		if (characterType.HasFlag(CharacterType.UppercaseLetter))
		{
			yield return char.IsUpper;
		}

		if (characterType.HasFlag(CharacterType.Digit))
		{
			yield return char.IsDigit;
		}

		if (characterType.HasFlag(CharacterType.Whitespace))
		{
			yield return char.IsWhiteSpace;
		}

		if (characterType.HasFlag(CharacterType.NonAlphanumericSymbol))
		{
			yield return c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c);
		}

		if (characterType.HasFlag(CharacterType.LowercaseLatinLetter))
		{
			yield return c => c is >= 'a' and <= 'z';
		}

		if (characterType.HasFlag(CharacterType.UppercaseLatinLetter))
		{
			yield return c => c is >= 'A' and <= 'Z';
		}
	}

	void OnCharacterTypePropertyChanged()
		=> characterPredicates = GetCharacterPredicates(CharacterType).ToList();

	bool Validate(string? value)
	{
		var count = value?.ToCharArray().Count(character => characterPredicates.Any(predicate => predicate.Invoke(character))) ?? 0;
		return count >= MinimumCharacterTypeCount
			&& count <= MaximumCharacterTypeCount;
	}
}