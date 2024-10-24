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

	void ReviewSummaryRatingChanged(object sender, RatingChangedEventArgs e)
	{
		ratings.Add(e.Rating);
		BindingContext.ReviewSummaryCount = ratings.Count;
		BindingContext.ReviewSummaryAverage = ratings.Average(x => x);
	}

	static void StreamMobileRate_Tapped(object sender, TappedEventArgs e)
	{
		_ = new CancellationTokenSource(TimeSpan.FromSeconds(5));
	}
}