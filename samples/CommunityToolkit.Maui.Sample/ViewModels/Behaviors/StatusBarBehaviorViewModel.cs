using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
public partial class StatusBarBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	int redSliderValue;
	
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	int greenSliderValue;
	
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	int blueSliderValue;

	public Color StatusBarColor => Color.FromRgb(RedSliderValue, GreenSliderValue, BlueSliderValue);
}
