using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Layouts;

public partial class StateLayoutViewModel : BaseViewModel
{
	[ObservableProperty]
	LayoutState currentState;

	[ObservableProperty]
	string? currentCustomStateKey;

	[ObservableProperty]
	LayoutState repeatingState;

	[RelayCommand]
	async Task CycleStates()
	{
		CurrentState = LayoutState.Loading;
		await Task.Delay(2000);

		CurrentState = LayoutState.Error;
		await Task.Delay(2000);

		CurrentState = LayoutState.Success;
		await Task.Delay(2000);

		CurrentState = LayoutState.Custom;
		CurrentCustomStateKey = "FirstCustomState";
		await Task.Delay(2000);

		CurrentCustomStateKey = "SecondCustomState";
		await Task.Delay(2000);

		CurrentState = LayoutState.None;
	}

	[RelayCommand]
	void ToggleRepeatingState()
	{
		if (RepeatingState == LayoutState.Loading)
		{
			RepeatingState = LayoutState.None;
		}
		else
		{
			RepeatingState = LayoutState.Loading;
		}
	}
}