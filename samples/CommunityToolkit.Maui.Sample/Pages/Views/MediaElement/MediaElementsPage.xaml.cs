using CommunityToolkit.Maui.Sample.ViewModels.Views;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaElementsPage : BasePage<MediaElementsViewModel>
{
	readonly IFileSystem fileSystem;
	readonly IDeviceInfo deviceInfo;
	readonly HttpClient httpClient;
	readonly ILogger<MediaElementPage> mediaElementPageLogger;
	readonly ILogger<MediaElementFromStreamPage> mediaElementFromStreamPageLogger;

	bool isOpenInProgress;

	public MediaElementsPage(
		MediaElementsViewModel mediaElementsViewModel,
		IFileSystem fileSystem,
		IDeviceInfo deviceInfo,
		HttpClient httpClient,
		ILogger<MediaElementPage> mediaElementPageLogger,
		ILogger<MediaElementFromStreamPage> mediaElementFromStreamPageLogger)
		: base(mediaElementsViewModel)
	{
		this.fileSystem = fileSystem;
		this.deviceInfo = deviceInfo;
		this.httpClient = httpClient;
		this.mediaElementPageLogger = mediaElementPageLogger;
		this.mediaElementFromStreamPageLogger = mediaElementFromStreamPageLogger;

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

	async void HandleMediaElementButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new MediaElementPage(new MediaElementViewModel(), fileSystem, deviceInfo, mediaElementPageLogger)));
	}

	async void HandleMediaElementFromStreamButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new MediaElementFromStreamPage(httpClient, new MediaElementFromStreamViewModel(), mediaElementFromStreamPageLogger)));
	}

	async void HandleMediaElementMultipleWindowsButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new MediaElementMultipleWindowsPage(new MediaElementMultipleWindowsViewModel())));
	}

	async void HandleMediaElementCarouselViewButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new MediaElementCarouselViewPage(new MediaElementCarouselViewViewModel())));
	}

	async void HandleMediaElementCollectionViewButtonClicked(object? sender, EventArgs e)
	{
		await OpenOneAtATimeAsync(() => Navigation.PushAsync(new MediaElementCollectionViewPage(new MediaElementCollectionViewViewModel())));
	}
}