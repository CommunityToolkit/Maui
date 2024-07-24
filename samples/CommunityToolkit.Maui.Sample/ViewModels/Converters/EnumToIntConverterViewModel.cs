using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class EnumToIntConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	IssueState selectedState = IssueState.None;

	public IReadOnlyList<string> AllStates { get; } = Enum.GetNames(typeof(IssueState));
}

public enum IssueState
{
	None = 0,
	New = 1,
	Open = 2,
	Waiting = 3,
	Developing = 4,
	WantFix = 5,
	Rejected = 6,
	Resolved = 7
}