using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
public partial class StatusBarBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	int redSliderValue, greenSliderValue, blueSliderValue;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarStyle))]
	bool isLightContentChecked, isDarkContentChecked, isDefaultChecked = true;

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

			throw new NotSupportedException($"{nameof(StatusBarStyle)} {StatusBarStyle} is not supported.");
		}
	}
}