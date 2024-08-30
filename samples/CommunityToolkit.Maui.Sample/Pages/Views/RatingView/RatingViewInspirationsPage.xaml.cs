using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class RatingViewInspirationsPage : BasePage<RatingViewInspirationsViewModel>
{
	public RatingViewInspirationsPage(RatingViewInspirationsViewModel viewModel) : base(viewModel) => InitializeComponent();

	void StreamMobileRate_Tapped(object sender, TappedEventArgs e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
	}
}