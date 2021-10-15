using System.Collections.Generic;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors
{
	public partial class CharactersValidationBehaviorPage
	{
		public CharactersValidationBehaviorPage()
		{
			InitializeComponent();
		}

		public List<CharacterType> CharacterTypes { get; } = new List<CharacterType>()
		{
			CharacterType.LowercaseLetter,
			CharacterType.UppercaseLetter,
			CharacterType.Letter,
			CharacterType.Digit,
			CharacterType.Alphanumeric,
			CharacterType.Whitespace,
			CharacterType.NonAlphanumericSymbol,
			CharacterType.LowercaseLatinLetter,
			CharacterType.UppercaseLatinLetter,
			CharacterType.LatinLetter,
			CharacterType.Any
		};
	}
}