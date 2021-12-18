using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class CharactersValidationBehaviorPage
{
	public CharactersValidationBehaviorPage() => InitializeComponent();

	public IReadOnlyList<CharacterType> CharacterTypes { get; } =
		Enum.GetValues(typeof(CharacterType)).Cast<CharacterType>().ToList();
}