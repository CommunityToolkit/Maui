using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class CharactersValidationBehaviorPage : BasePage<CharactersValidationBehaviorViewModel>
{
	public CharactersValidationBehaviorPage(IDeviceInfo deviceInfo, CharactersValidationBehaviorViewModel charactersValidationBehaviorViewModel)
		: base(deviceInfo, charactersValidationBehaviorViewModel)
	{
		InitializeComponent();
	}

	public IReadOnlyList<CharacterType> CharacterTypes { get; } =
		Enum.GetValues(typeof(CharacterType)).Cast<CharacterType>().ToList();
}