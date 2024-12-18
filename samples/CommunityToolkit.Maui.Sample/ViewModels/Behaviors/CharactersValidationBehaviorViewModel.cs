using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class CharactersValidationBehaviorViewModel : BaseViewModel
{
	public IReadOnlyList<CharacterType> CharacterTypes { get; } = [.. Enum.GetValues<CharacterType>()];
}