using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Layouts;

public partial class StateContainerViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string? CurrentState { get; set; }

	[ObservableProperty]
	public partial string? GridState { get; set; }

	[ObservableProperty]
	public partial string? NoAnimateState { get; set; }

	[ObservableProperty]
	public partial string? NotFoundState { get; set; }

	[ObservableProperty]
	public partial string? FullPageState { get; set; }

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(ToggleGridStateCommand))]
	public partial bool CanGridStateChange { get; set; } = true;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(CycleStatesCommand))]
	public partial bool CanCycleStateChange { get; set; } = true;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(ToggleFullPageStateCommand))]
	public partial bool CanFullPageStateChange { get; set; } = true;

	[ObservableProperty]
	public partial bool CanAnimationStateChange { get; set; } = true;

	[RelayCommand(CanExecute = nameof(CanCycleStateChange))]
	async Task CycleStates(CancellationToken token)
	{
		CurrentState = StateKey.Loading;
		await Task.Delay(2000, token).ConfigureAwait(false);

		CurrentState = StateKey.Success;
		await Task.Delay(2000, token).ConfigureAwait(false);

		CurrentState = StateKey.Anything;
		await Task.Delay(2000, token).ConfigureAwait(false);

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