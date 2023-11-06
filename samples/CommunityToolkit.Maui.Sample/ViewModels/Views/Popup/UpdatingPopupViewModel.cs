using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class UpdatingPopupViewModel : BaseViewModel
{
	const double finalUpdateProgressValue = 1;

	readonly WeakEventManager finishedEventManager = new();

	[ObservableProperty]
	string message = "";

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(FinishCommand))]
	double updateProgress;

	public event EventHandler<EventArgs> Finished
	{
		add => finishedEventManager.AddEventHandler(value);
		remove => finishedEventManager.RemoveEventHandler(value);
	}

	internal async void PerformUpdates(int numberOfUpdates)
	{
		double updateTotalForPercentage = numberOfUpdates + 1;

		for (var update = 1; update <= numberOfUpdates; update++)
		{
			Message = $"Updating {update} of {numberOfUpdates}";

			UpdateProgress = update / updateTotalForPercentage;

			await Task.Delay(TimeSpan.FromSeconds(1));
		}

		UpdateProgress = finalUpdateProgressValue;
		Message = "Updates complete";
	}

	[RelayCommand(CanExecute = nameof(CanFinish))]
	void OnFinish()
	{
		finishedEventManager.HandleEvent(this, EventArgs.Empty, nameof(Finished));
	}

	bool CanFinish() => UpdateProgress is finalUpdateProgressValue;
}