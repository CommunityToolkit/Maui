namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class RatingViewShowcaseViewModel : BaseViewModel
{
	public double StepperValueMaximumRatings
	{
		get;
		set => SetProperty(ref field, value);
	} = 1;

	public double ReviewSummaryAverage
	{
		get;
		set => SetProperty(ref field, value);
	} = 0;

	Thickness ratingViewShapePadding = new(0);

	public Thickness RatingViewShapePadding
	{
		get => ratingViewShapePadding;
		set => SetProperty(ref ratingViewShapePadding, value);
	}

	public double ReviewSummaryCount
	{
		get;
		set => SetProperty(ref field, value);
	} = 0;
}