using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MapsPage : BasePage<MapsViewModel>
{
	bool isOpenInProgress;

	public MapsPage(MapsViewModel mapsViewModel)
		: base(mapsViewModel)
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

	async void HandleBasicMapsButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new BasicMapsPage(new BasicMapsViewModel())));
	}

	async void HandleMapsPinsButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new MapsPinsPage(new MapsPinsViewModel())));
	}
}