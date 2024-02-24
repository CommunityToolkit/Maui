using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.PlatformSpecific;
public partial class NavigationBarAndroidViewModel : BaseViewModel
{
	[ObservableProperty, NotifyPropertyChangedFor(nameof(NavigationBarColor))]
	int redSliderValue, greenSliderValue, blueSliderValue;

	[ObservableProperty, NotifyPropertyChangedFor(nameof(NavigationBarStyle))]
	bool isLightContentChecked, isDarkContentChecked, isDefaultChecked = true;

	public Color NavigationBarColor => Color.FromRgb(RedSliderValue, GreenSliderValue, BlueSliderValue);

	public NavigationBarStyle NavigationBarStyle
	{
		get
		{
			if (IsDefaultChecked)
			{
				return NavigationBarStyle.Default;
			}
			if (IsLightContentChecked)
			{
				return NavigationBarStyle.LightContent;
			}
			if (IsDarkContentChecked)
			{
				return NavigationBarStyle.DarkContent;
			}

			throw new NotSupportedException($"{nameof(NavigationBarStyle)} {NavigationBarStyle} is not supported.");
		}
	}
}