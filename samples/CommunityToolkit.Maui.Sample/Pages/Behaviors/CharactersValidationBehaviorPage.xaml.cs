using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class CharactersValidationBehaviorPage : BasePage<CharactersValidationBehaviorViewModel>
{
	public CharactersValidationBehaviorPage(CharactersValidationBehaviorViewModel charactersValidationBehaviorViewModel)
		: base(charactersValidationBehaviorViewModel)
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