using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Behaviors
{
	/// <summary>
	/// The <see cref="CharactersValidationBehavior"/> is a behavior that allows the user to validate text input depending on specified parameters.For example, an <see cref="Entry"/> control can be styled differently depending on whether a valid or an invalid text value is provided. This behavior includes built-in checks such as checking for a certain number of digits or alphanumeric characters. Additional properties handling validation are inherited from <see cref="Internals.ValidationBehavior"/>.
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
		/// Backing BindableProperty for the <see cref="MinimumCharacterCount"/> property.
		/// </summary>
		public static readonly BindableProperty MinimumCharacterCountProperty =
			BindableProperty.Create(nameof(MinimumCharacterCount), typeof(int), typeof(CharactersValidationBehavior), 0, propertyChanged: OnValidationPropertyChanged);

		/// <summary>
		/// Backing BindableProperty for the <see cref="MaximumCharacterCount"/> property.
		/// </summary>
		public static readonly BindableProperty MaximumCharacterCountProperty =
			BindableProperty.Create(nameof(MaximumCharacterCount), typeof(int), typeof(CharactersValidationBehavior), int.MaxValue, propertyChanged: OnValidationPropertyChanged);

		/// <summary>
		/// Constructor for this behavior
		/// </summary>
		public CharactersValidationBehavior()
			=> OnCharacterTypePropertyChanged();

		/// <summary>
		/// Provides an enumerated value to use to set how to handle comparisons. This is a bindable property.
		/// </summary>
		public CharacterType CharacterType
		{
			get => (CharacterType)GetValue(CharacterTypeProperty);
			set => SetValue(CharacterTypeProperty, value);
		}

		/// <summary>
		/// The minimum length of the text input that's allowed. This is a bindable property.
		/// </summary>
		public int MinimumCharacterCount
		{
			get => (int)GetValue(MinimumCharacterCountProperty);
			set => SetValue(MinimumCharacterCountProperty, value);
		}

		/// <summary>
		/// The maximum length of the text input that's allowed. This is a bindable property.
		/// </summary>
		public int MaximumCharacterCount
		{
			get => (int)GetValue(MaximumCharacterCountProperty);
			set => SetValue(MaximumCharacterCountProperty, value);
		}

		protected override async ValueTask<bool> ValidateAsync(object? value, CancellationToken token)
			=> await base.ValidateAsync(value, token).ConfigureAwait(false)
				&& Validate(value?.ToString());

		static void OnCharacterTypePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			((CharactersValidationBehavior)bindable).OnCharacterTypePropertyChanged();
			OnValidationPropertyChanged(bindable, oldValue, newValue);
		}

		static IEnumerable<Predicate<char>> GetCharacterPredicates(CharacterType characterType)
		{
			if (characterType.HasFlag(CharacterType.LowercaseLetter))
				yield return char.IsLower;

			if (characterType.HasFlag(CharacterType.UppercaseLetter))
				yield return char.IsUpper;

			if (characterType.HasFlag(CharacterType.Digit))
				yield return char.IsDigit;

			if (characterType.HasFlag(CharacterType.Whitespace))
				yield return char.IsWhiteSpace;

			if (characterType.HasFlag(CharacterType.NonAlphanumericSymbol))
				yield return c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c);

			if (characterType.HasFlag(CharacterType.LowercaseLatinLetter))
				yield return c => c >= 'a' && c <= 'z';

			if (characterType.HasFlag(CharacterType.UppercaseLatinLetter))
				yield return c => c >= 'A' && c <= 'Z';
		}

		void OnCharacterTypePropertyChanged()
			=> characterPredicates = GetCharacterPredicates(CharacterType).ToList();

		bool Validate(string? value)
		{
			var count = value?.ToCharArray().Count(character => characterPredicates.Any(predicate => predicate.Invoke(character))) ?? 0;
			return count >= MinimumCharacterCount
				&& count <= MaximumCharacterCount;
		}
	}
}