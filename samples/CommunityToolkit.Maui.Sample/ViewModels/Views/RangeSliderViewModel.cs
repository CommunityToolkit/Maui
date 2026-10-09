using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class RangeSliderViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial double SimpleLowerValue { get; set; } = 125;

	[ObservableProperty]
	public partial double SimpleUpperValue { get; set; } = 175;

	[ObservableProperty]
	public partial double LowerValueWithCustomStyle { get; set; } = 125;

	[ObservableProperty]
	public partial double UpperValueWithCustomStyle { get; set; } = 175;

	[ObservableProperty]
	public partial double LowerValueWithStep { get; set; } = 125;

	[ObservableProperty]
	public partial double UpperValueWithStep { get; set; } = 175;

	[ObservableProperty]
	public partial double LowerValueDescending { get; set; } = 175;

	[ObservableProperty]
	public partial double UpperValueDescending { get; set; } = 125;

	[ObservableProperty]
	public partial double LowerValueDescendingWithStep { get; set; } = 175;

	[ObservableProperty]
	public partial double UpperValueDescendingWithStep { get; set; } = 125;
}