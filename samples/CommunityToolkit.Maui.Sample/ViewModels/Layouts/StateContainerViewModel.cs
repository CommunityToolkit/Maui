using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Layouts;

public partial class StateContainerViewModel : BaseViewModel
{
	class StateKey
	{
		public const string Loading = "Loading";
		public const string Success = "Success";
		public const string Anything = "StateKey can be anything!";
		public const string ReplaceGrid = "ReplaceGrid";
		public const string NoAnimate = "NoAnimate";
		public const string NotFound = "NotFoundExampleKey";
	}

	[ObservableProperty]
	string? currentState;

	[ObservableProperty]
	string? gridState;

	[ObservableProperty]
	string? noAnimateState;

	[ObservableProperty]
	string? notFoundState;

	[RelayCommand]
	async Task CycleStates()
	{
		CurrentState = StateKey.Loading;
		await Task.Delay(2000);

		CurrentState = StateKey.Success;
		await Task.Delay(2000);

		CurrentState = StateKey.Anything;
		await Task.Delay(2000);

		// Setting state to empty string or null returns to the default content
		CurrentState = string.Empty;
	}

	[RelayCommand]
	void ToggleGridState()
	{
		if (GridState == StateKey.ReplaceGrid)
		{
			GridState = null;
		}
		else
		{
			GridState = StateKey.ReplaceGrid;
		}
	}

	[RelayCommand]
	void ToggleNoAnimateState()
	{
		if (NoAnimateState == StateKey.NoAnimate)
		{
			NoAnimateState = null;
		}
		else
		{
			NoAnimateState = StateKey.NoAnimate;
		}
	}

	[RelayCommand]
	void ToggleNotFoundState()
	{
		if (NotFoundState == StateKey.NotFound)
		{
			NotFoundState = null;
		}
		else
		{
			NotFoundState = StateKey.NotFound;
		}
	}
}