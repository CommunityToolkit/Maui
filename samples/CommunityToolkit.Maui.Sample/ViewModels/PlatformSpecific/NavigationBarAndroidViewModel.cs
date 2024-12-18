using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.PlatformSpecific;

public partial class NavigationBarAndroidViewModel : BaseViewModel
{
	[ObservableProperty, NotifyPropertyChangedFor(nameof(NavigationBarColor))]
	public partial int RedSliderValue { get; set; }

	[ObservableProperty, NotifyPropertyChangedFor(nameof(NavigationBarColor))]
	public partial int GreenSliderValue { get; set; }

	[ObservableProperty, NotifyPropertyChangedFor(nameof(NavigationBarColor))]
	public partial int BlueSliderValue { get; set; }

	[ObservableProperty, NotifyPropertyChangedFor(nameof(NavigationBarStyle))]
	public partial bool IsLightContentChecked { get; set; }

	[ObservableProperty, NotifyPropertyChangedFor(nameof(NavigationBarStyle))]
	public partial bool IsDarkContentChecked { get; set; }

	[ObservableProperty, NotifyPropertyChangedFor(nameof(NavigationBarStyle))]
	public partial bool IsDefaultChecked { get; set; } = true;

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