using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Layouts;

public partial class StateContainerViewModel : BaseViewModel
{
	[ObservableProperty]
	string? currentState;

	[ObservableProperty]
	string? gridState;

	[RelayCommand]
	async Task CycleStates()
	{
		CurrentState = "Loading";
		await Task.Delay(2000);

		CurrentState = "Success";
		await Task.Delay(2000);

		CurrentState = "AnotherState";
		await Task.Delay(2000);

		CurrentState = "StateKey can be anything!";
		await Task.Delay(2000);

		// Setting state to empty string or null returns to the default content
		CurrentState = string.Empty;
	}

	[RelayCommand]
	async Task CycleGridStates()
	{
		GridState = "ReplaceGridState";
		await Task.Delay(2000);

		GridState = null;
	}
}