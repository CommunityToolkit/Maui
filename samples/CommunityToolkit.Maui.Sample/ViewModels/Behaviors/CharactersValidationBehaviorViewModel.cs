using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class CharactersValidationBehaviorViewModel : BaseViewModel
{
	public IReadOnlyList<CharacterType> CharacterTypes { get; } = Enum.GetValues(typeof(CharacterType)).Cast<CharacterType>().ToList();
}