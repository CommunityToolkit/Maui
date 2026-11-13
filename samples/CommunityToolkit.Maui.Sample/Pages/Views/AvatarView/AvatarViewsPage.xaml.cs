using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class AvatarViewsPage : BasePage<AvatarViewsViewModel>
{
	bool isOpenInProgress;

	public AvatarViewsPage(AvatarViewsViewModel avatarViewsViewModel)
		: base(avatarViewsViewModel)
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

	async void HandleAvatarViewBindablePropertiesButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new AvatarViewBindablePropertiesPage(new AvatarViewBindablePropertiesViewModel())));
	}

	async void HandleAvatarViewBordersButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new AvatarViewBordersPage(new AvatarViewBordersViewModel())));
	}

	async void HandleAvatarViewColorsButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new AvatarViewColorsPage(new AvatarViewColorsViewModel())));
	}

	async void HandleAvatarViewDayOfWeekButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new AvatarViewDayOfWeekPage(new AvatarViewDayOfWeekViewModel())));
	}

	async void HandleAvatarViewGesturesButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new AvatarViewGesturesPage(new AvatarViewGesturesViewModel())));
	}

	async void HandleAvatarViewImagesButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new AvatarViewImagesPage(new AvatarViewImagesViewModel())));
	}

	async void HandleAvatarViewKeyboardButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new AvatarViewKeyboardPage(new AvatarViewKeyboardViewModel())));
	}

	async void HandleAvatarViewRatingButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new AvatarViewRatingPage(new AvatarViewRatingViewModel())));
	}

	async void HandleAvatarViewShadowsButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new AvatarViewShadowsPage(new AvatarViewShadowsViewModel())));
	}

	async void HandleAvatarViewShapesButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new AvatarViewShapesPage(new AvatarViewShapesViewModel())));
	}

	async void HandleAvatarViewSizesButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new AvatarViewSizesPage(new AvatarViewSizesViewModel())));
	}
}