using System;

namespace CommunityToolkit.Maui.Behaviors
{
	/// <summary>
	/// The allowed character types used to determine if a value is valid in the <see cref="CharactersValidationBehavior"/>. Since this is a flag, multiple flags cane be combined.
	/// </summary>
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
}