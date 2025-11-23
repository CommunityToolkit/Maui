using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class StatusBarBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	public partial double RedSliderValue { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	public partial double GreenSliderValue { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	public partial double BlueSliderValue { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	public partial double AlphaSliderValue { get; set; } = 1;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarStyle))]
	public partial bool IsLightContentChecked { get; set; } = true;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarStyle))]
	public partial bool IsDarkContentChecked { get; set; } = true;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarStyle))]
	public partial bool IsDefaultChecked { get; set; } = true;

	public Color StatusBarColor => Color.FromRgba(RedSliderValue, GreenSliderValue, BlueSliderValue, AlphaSliderValue);

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