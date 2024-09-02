using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class RatingViewShowcasePage : BasePage<RatingViewShowcaseViewModel>
{
	public RatingViewShowcasePage(RatingViewShowcaseViewModel viewModel) : base(viewModel) => InitializeComponent();

	void StreamMobileRate_Tapped(object sender, TappedEventArgs e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
	}
}