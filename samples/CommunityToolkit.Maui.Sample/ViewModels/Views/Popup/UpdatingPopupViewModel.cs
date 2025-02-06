﻿using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class UpdatingPopupViewModel(IPopupService popupService) : BaseViewModel
{
	const double finalUpdateProgressValue = 1;

	[ObservableProperty]
	public partial string Message { get; set; } = "";

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(FinishCommand))]
	public partial double UpdateProgress { get; set; }

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
		Message = $"Completed {numberOfUpdates} updates";
	}

	[RelayCommand(CanExecute = nameof(CanFinish))]
	void OnFinish()
	{
		popupService.ClosePopup();
	}

	[RelayCommand]
	async Task OnMore()
	{
		await popupService.ShowPopupAsync<UpdatingPopupViewModel>(new PopupOptions(), CancellationToken.None);
	}

	bool CanFinish() => UpdateProgress is finalUpdateProgressValue;
}