using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class MaxLengthReachedBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string CommandExecutionText { get; set; } = string.Empty;

	[RelayCommand]
	void MaxLengthReached(string text)
		=> CommandExecutionText += string.Format("MaxLength reached with value: '{0}'.", text) + Environment.NewLine;
}