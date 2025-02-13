using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class RatingViewXamlPage : BasePage<RatingViewXamlViewModel>
{
	public RatingViewXamlPage(RatingViewXamlViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	// This is event handler demonstrates the RatingChanged event is raised when a user modifies the rating
	async void HandleRatingChanged(object? sender, RatingChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var ratingView = (RatingView)sender;
		await Toast.Make($"New Rating: {ratingView.Rating:F2}").Show(CancellationToken.None);
	}
}