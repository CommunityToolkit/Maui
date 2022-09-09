using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
public partial class StatusBarBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarStyle))]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	int redSliderValue;
	
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarStyle))]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	int greenSliderValue;
	
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarStyle))]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	int blueSliderValue;
	
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarStyle))]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	bool isDefaultChecked = true;
	
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarStyle))]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	bool isLightContentChecked;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarStyle))]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	bool isDarkContentChecked;

	public Color StatusBarColor => Color.FromRgb(RedSliderValue, GreenSliderValue, BlueSliderValue);

	public StatusBarStyle StatusBarStyle
	{
		get
		{
			if (IsDefaultChecked)
			{
				return StatusBarStyle.Default;
			}
			if (IsLightContentChecked)
			{
				return StatusBarStyle.LightContent;
			}
			if (IsDarkContentChecked)
			{
				return StatusBarStyle.DarkContent;
			}
			throw new InvalidOperationException("Style is not selected.");
		}
	}
}
