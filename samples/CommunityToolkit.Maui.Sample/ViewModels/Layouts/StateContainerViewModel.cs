using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Layouts;

public partial class StateContainerViewModel : BaseViewModel
{
	[ObservableProperty]
	string? currentState, gridState, noAnimateState, notFoundState, fullPageState;

	[ObservableProperty]
	bool canGridStateChange = true, canCycleStateChange = true, canAnimationStateChange = true, canFullPageStateChange = true;

	[RelayCommand(CanExecute = nameof(CanCycleStateChange))]
	async Task CycleStates()
	{
		CurrentState = StateKey.Loading;
		await Task.Delay(2000).ConfigureAwait(false);

		CurrentState = StateKey.Success;
		await Task.Delay(2000).ConfigureAwait(false);

		CurrentState = StateKey.Anything;
		await Task.Delay(2000).ConfigureAwait(false);

		// Setting state to empty string or null returns to the default content
		CurrentState = string.Empty;
	}

	[RelayCommand(CanExecute = nameof(CanGridStateChange))]
	void ToggleGridState() => GridState = GridState switch
	{
		StateKey.ReplaceGrid => null,
		_ => StateKey.ReplaceGrid
	};

	[RelayCommand]
	void ToggleNotFoundState() => NotFoundState = NotFoundState switch
	{
		StateKey.NotFound => null,
		_ => StateKey.NotFound
	};

	[RelayCommand(CanExecute = nameof(CanFullPageStateChange))]
	void ToggleFullPageState() => FullPageState = FullPageState switch
	{
		StateKey.Loading => null,
		_ => StateKey.Loading
	};

	partial void OnCanGridStateChangeChanged(bool value)
	{
		ToggleGridStateCommand.NotifyCanExecuteChanged();
	}

	partial void OnCanCycleStateChangeChanged(bool value)
	{
		CycleStatesCommand.NotifyCanExecuteChanged();
	}

	partial void OnCanFullPageStateChangeChanged(bool value)
	{
		ToggleFullPageStateCommand.NotifyCanExecuteChanged();
	}

	static class StateKey
	{
		public const string Loading = nameof(Loading);
		public const string Success = nameof(Success);
		public const string Anything = "StateKey can be anything!";
		public const string ReplaceGrid = nameof(ReplaceGrid);
		public const string NoAnimate = nameof(NoAnimate);
		public const string NotFound = nameof(NotFound);
	}
}