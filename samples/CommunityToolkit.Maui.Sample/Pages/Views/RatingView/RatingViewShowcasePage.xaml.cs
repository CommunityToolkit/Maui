using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class RatingViewShowcasePage : BasePage<RatingViewShowcaseViewModel>
{
	readonly List<double> ratings = [];
	readonly RatingViewShowcaseViewModel viewModel;

	public RatingViewShowcasePage(RatingViewShowcaseViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
		this.viewModel = viewModel;
	}

	void ReviewSummaryRatingChanged(object sender, RatingChangedEventArgs e)
	{
		ratings.Add(e.Rating);
		viewModel.ReviewSummaryCount = ratings.Count;
		viewModel.ReviewSummaryAverage = ratings.Average(x => x);
	}
}