using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class TouchBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial int TouchCount { get; private set; }

	[ObservableProperty]
	public partial int LongPressCount { get; private set; }

	static Task DisplayAlertAsync(string title, CancellationToken token)
		=> Shell.Current.DisplayAlertAsync(title, null, "Ok").WaitAsync(token);

	[RelayCommand]
	static Task ParentClicked(CancellationToken token)
		=> DisplayAlertAsync("Parent Clicked", token);

	[RelayCommand]
	static Task ChildClicked(CancellationToken token)
		=> DisplayAlertAsync("Child Clicked", token);

	[RelayCommand]
	async Task MonkeySelected(string? monkey, CancellationToken token)
	{
		if (string.IsNullOrEmpty(monkey))
		{
			await DisplayAlertAsync("No monkey selected", token);
			return;
		}

		await DisplayAlertAsync($"Selected monkey: {monkey}", token);
	}

	[RelayCommand]
	void IncreaseTouchCount()
	{
		TouchCount++;
	}

	[RelayCommand]
	void IncreaseLongPressCount()
	{
		LongPressCount++;
	}
}