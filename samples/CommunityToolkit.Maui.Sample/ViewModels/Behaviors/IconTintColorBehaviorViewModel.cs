using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class IconTintColorBehaviorViewModel : BaseViewModel
{
	[RelayCommand]
	void Change()
	{
		IsChanged = !IsChanged;
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ChangedColor))]
	bool isChanged;

	public Color ChangedColor => IsChanged ? Colors.Red : Colors.Green;
}