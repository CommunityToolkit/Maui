namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

using Mvvm.ComponentModel;

public partial class RatingViewShowcaseViewModel : BaseViewModel
{
	[ObservableProperty]
	double stepperValueMaximumRatings = 1, reviewSummaryAverage = 0;

	[ObservableProperty]
	Thickness ratingViewShapePadding = new(0);

	[ObservableProperty]
	int reviewSummaryCount;
}