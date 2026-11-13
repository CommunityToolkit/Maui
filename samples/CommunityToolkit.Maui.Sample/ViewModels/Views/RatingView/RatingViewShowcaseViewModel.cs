using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class RatingViewShowcaseViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial double StepperValueMaximumRatings { get; set; } = 1;

	[ObservableProperty]
	public partial double ReviewSummaryAverage { get; set; } = 0;

	[ObservableProperty]
	public partial Thickness RatingViewShapePadding { get; set; } = new(0);

	[ObservableProperty]
	public partial double ReviewSummaryCount { get; set; } = 0;
}