using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class RatingViewsPage : BasePage<RatingViewsViewModel>
{
	bool isOpenInProgress;

	public RatingViewsPage(RatingViewsViewModel ratingViewsViewModel)
		: base(ratingViewsViewModel)
	{
		InitializeComponent();
	}

	// A page covers this page only once its push is done, so quick taps, a repeated key or UI Automation would open a second one
	async Task OpenOneAtATimeAsync(Func<Task> open)
	{
		if (isOpenInProgress)
		{
			return;
		}

		isOpenInProgress = true;

		try
		{
			await open();
		}
		finally
		{
			isOpenInProgress = false;
		}
	}

	async void HandleRatingViewCsharpButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new RatingViewCsharpPage(new RatingViewCsharpViewModel())));
	}

	async void HandleRatingViewShowcaseButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new RatingViewShowcasePage(new RatingViewShowcaseViewModel())));
	}

	async void HandleRatingViewXamlButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new RatingViewXamlPage(new RatingViewXamlViewModel())));
	}
}