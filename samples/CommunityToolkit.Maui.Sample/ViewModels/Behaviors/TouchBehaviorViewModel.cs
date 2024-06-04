using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class TouchBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	int touchCount, longPressCount;
	
	public ObservableCollection<ItemViewModel> Items { get; }

	static Task DisplayAlert(string title, CancellationToken token)
		=> Shell.Current.DisplayAlert(title, null, "Ok").WaitAsync(token);

	[RelayCommand]
	static Task ParentClicked(CancellationToken token)
		=> DisplayAlert("Parent Clicked", token);

	[RelayCommand]
	static Task ChildClicked(CancellationToken token)
		=> DisplayAlert("Child Clicked", token);

	public TouchBehaviorViewModel()
	{
		Items = [];
		for (var i = 0; i < 50; ++i)
		{
			Items.Add(new ItemViewModel($"Item {i}"));
		}
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
