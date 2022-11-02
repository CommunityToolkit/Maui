using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Layouts;

public partial class StateContainerViewModel : BaseViewModel
{
	[ObservableProperty]
	string? currentState;

	[ObservableProperty]
	string? gridState;

	[ObservableProperty]
	string? noAnimateState;

	[ObservableProperty]
	string? notFoundState;

	[ObservableProperty]
	string? fullPageState;

	[RelayCommand]
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

	[RelayCommand]
	void ToggleGridState() => GridState = GridState switch
	{
		StateKey.ReplaceGrid => null,
		_ => StateKey.ReplaceGrid
	};

	[RelayCommand]
	void ToggleNoAnimateState() => NoAnimateState = NoAnimateState switch
	{
		StateKey.NoAnimate => null,
		_ => StateKey.NoAnimate
	};

	[RelayCommand]
	void ToggleNotFoundState() => NotFoundState = NotFoundState switch
	{
		StateKey.NotFound => null,
		_ => StateKey.NotFound
	};

	[RelayCommand]
	void ToggleFullPageState() => FullPageState = FullPageState switch
	{
		StateKey.Loading => null,
		_ => StateKey.Loading
	};

	static class StateKey
	{
		public const string Loading = "Loading";
		public const string Success = "Success";
		public const string Anything = "StateKey can be anything!";
		public const string ReplaceGrid = "ReplaceGrid";
		public const string NoAnimate = "NoAnimate";
		public const string NotFound = "NotFoundExampleKey";
	}
}