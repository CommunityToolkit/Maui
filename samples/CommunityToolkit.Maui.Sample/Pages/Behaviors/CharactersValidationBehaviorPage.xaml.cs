using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class CharactersValidationBehaviorPage : BasePage
{
	public CharactersValidationBehaviorPage()
	{
		InitializeComponent();
	}

	public IReadOnlyList<CharacterType> CharacterTypes { get; } =
		Enum.GetValues(typeof(CharacterType)).Cast<CharacterType>().ToList();
}