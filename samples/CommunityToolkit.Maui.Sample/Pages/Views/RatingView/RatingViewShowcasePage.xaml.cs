using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class RatingViewShowcasePage : BasePage<RatingViewShowcaseViewModel>
{
	readonly List<double> ratings = [];

	public RatingViewShowcasePage(RatingViewShowcaseViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	void ReviewSummaryRatingChanged(object? sender, RatingChangedEventArgs? e)
	{
		ratings.Add(e?.Rating ?? 0);
		if (BindingContext is RatingViewShowcaseViewModel viewModel)
		{
			viewModel.ReviewSummaryCount = ratings.Count;
			viewModel.ReviewSummaryAverage = ratings.Average(x => x);
		}
	}
}