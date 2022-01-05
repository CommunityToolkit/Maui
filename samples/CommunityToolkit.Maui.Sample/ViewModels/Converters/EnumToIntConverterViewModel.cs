using System;
using System.Collections.Generic;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class EnumToIntConverterViewModel : BaseViewModel
{
	IssueState selectedState = IssueState.None;

	public IReadOnlyList<string> AllStates { get; } = Enum.GetNames(typeof(IssueState));

	public IssueState SelectedState
	{
		get => selectedState;
		set => SetProperty(ref selectedState, value);
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
}