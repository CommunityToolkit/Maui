using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui;

static class CharactersValidationBehaviorDefaults
{
	public const int MaximumCharacterTypeCount = int.MaxValue;
	public const int MinimumCharacterTypeCount = 0;

	public static CharacterType CharacterType { get; } = CharacterType.Any;
}