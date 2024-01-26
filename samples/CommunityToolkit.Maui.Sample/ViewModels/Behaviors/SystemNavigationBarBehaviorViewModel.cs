using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
public partial class SystemNavigationBarBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SystemNavigationBarColor))]
	int redSliderValue, greenSliderValue, blueSliderValue;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SystemNavigationBarStyle))]
	bool isLightContentChecked, isDarkContentChecked, isDefaultChecked = true;

	public Color SystemNavigationBarColor => Color.FromRgb(RedSliderValue, GreenSliderValue, BlueSliderValue);

	public SystemNavigationBarStyle SystemNavigationBarStyle
	{
		get
		{
			if (IsDefaultChecked)
			{
				return SystemNavigationBarStyle.Default;
			}
			if (IsLightContentChecked)
			{
				return SystemNavigationBarStyle.LightContent;
			}
			if (IsDarkContentChecked)
			{
				return SystemNavigationBarStyle.DarkContent;
			}

			throw new NotSupportedException($"{nameof(SystemNavigationBarStyle)} {SystemNavigationBarStyle} is not supported.");
		}
	}
}