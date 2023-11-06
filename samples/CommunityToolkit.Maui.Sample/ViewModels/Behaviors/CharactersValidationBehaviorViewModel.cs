using System.Collections.Frozen;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class CharactersValidationBehaviorViewModel : BaseViewModel
{
	public FrozenSet<CharacterType> CharacterTypes { get; } =
		Enum.GetValues(typeof(CharacterType)).Cast<CharacterType>().ToFrozenSet();
}