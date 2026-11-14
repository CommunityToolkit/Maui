namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class ValuePropertyNameValidationBehaviorViewModel : BaseViewModel
{
	public IReadOnlyList<string> Plans { get; } = ["Free", "Pro", "Enterprise"];
}