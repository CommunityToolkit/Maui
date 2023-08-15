using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class UpdatingPopupViewModel : BaseViewModel
{
	[Mvvm.ComponentModel.ObservableProperty]
	string message = "";

	[Mvvm.ComponentModel.ObservableProperty]
	[Mvvm.ComponentModel.NotifyCanExecuteChangedFor(nameof(FinishCommand))]
	double updateProgress;

	public UpdatingPopupViewModel()
	{
	}

	internal async void PerformUpdates(int numberOfUpdates)
	{
		double updateTotalForPercentage = numberOfUpdates + 1;

		for (var update = 1; update <= numberOfUpdates; update++)
		{
			this.Message = $"Updating {update} of {numberOfUpdates}";

			this.UpdateProgress = update / updateTotalForPercentage;

			await Task.Delay(TimeSpan.FromSeconds(3));	
		}

		this.UpdateProgress = 1d;
		this.Message = "Updates complete";
	}

	[RelayCommand(CanExecute = nameof(CanFinish))]
	void OnFinish()
	{

	}

	bool CanFinish() => this.UpdateProgress == 1d;
}
