using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class CharactersValidationBehaviorPage
{
	public CharactersValidationBehaviorPage()
	{
		InitializeComponent();

		Page ??= this;
		CharacterTypePicker ??= new Picker();
		MinimumCharacterCountEntry ??= new Entry();
		MaximumCharacterCountEntry ??= new Entry();
	}

	public IReadOnlyList<CharacterType> CharacterTypes { get; } = 
		Enum.GetValues(typeof(CharacterType)).Cast<CharacterType>().ToList();
}