using System.ComponentModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.Constants;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public sealed partial class MediaElementFromStreamPage : BasePage<MediaElementFromStreamViewModel>, IAsyncDisposable
{
	readonly ILogger logger;
	readonly HttpClient httpClient;
	readonly SemaphoreSlim sourceStreamSemaphore = new(1, 1);

	MemoryStream? sourceStream;

	public MediaElementFromStreamPage(
		HttpClient httpClient,
		MediaElementFromStreamViewModel viewModel,
		ILogger<MediaElementFromStreamPage> logger) : base(viewModel)
	{
		InitializeComponent();

		this.logger = logger;
		this.httpClient = httpClient;
		MediaElement.PropertyChanged += MediaElement_PropertyChanged;
	}

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		await ReleaseSourceStream();
	}

	protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
	{
		base.OnNavigatedFrom(args);
		MediaElement.Stop();

		// Release the player only when the page leaves the navigation stack. When another flyout item is selected (NavigationType.Replace),
		// Shell keeps the page in the navigation stack of the Views item and shows it again, with this MediaElement, when the user comes back
		if (args.NavigationType is NavigationType.Pop or NavigationType.PopToRoot or NavigationType.Remove)
		{
			MediaElement.Handler?.DisconnectHandler();
		}
	}

	protected override async void OnDisappearing()
	{
		base.OnDisappearing();

		// In production, consider warning the user before navigating away
		// if an unsaved stream is loaded (e.g. after video capture).
		// Here we simply release the stream on disappearing for simplicity.
		await ReleaseSourceStream();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		try
		{
			await using var networkStream = await httpClient.GetStreamAsync(StreamingUrls.BuckBunny);
			await UpdateSourceStream(networkStream);
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
		{
			// For example, the device is offline (HttpRequestException) or the download timed out (TaskCanceledException)
			logger.LogError(ex, "Failed to download the video");
			await Toast.Make("Failed to download the video. Check the internet connection.").Show();
		}
	}

	void MediaElement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == MediaElement.DurationProperty.PropertyName)
		{
			logger.LogInformation("Duration: {NewDuration}", MediaElement.Duration);
			PositionSlider.Maximum = MediaElement.Duration.TotalSeconds;
		}
	}

	void OnMediaOpened(object? sender, EventArgs? e) => logger.LogInformation("Media opened.");

	void OnStateChanged(object? sender, MediaStateChangedEventArgs e) =>
		logger.LogInformation("Media State Changed. Old State: {PreviousState}, New State: {NewState}", e.PreviousState, e.NewState);

	void OnMediaFailed(object? sender, MediaFailedEventArgs e) => logger.LogInformation("Media failed. Error: {ErrorMessage}", e.ErrorMessage);

	void OnMediaEnded(object? sender, EventArgs? e) => logger.LogInformation("Media ended.");

	void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e)
	{
		logger.LogInformation("Position changed to {Position}", e.Position);
		PositionSlider.Value = e.Position.TotalSeconds;
	}

	void OnSeekCompleted(object? sender, EventArgs? e) => logger.LogInformation("Seek completed.");

	void OnSpeedMinusClicked(object? sender, EventArgs? e)
	{
		if (MediaElement.Speed >= 1)
		{
			MediaElement.Speed -= 1;
		}
	}

	void OnSpeedPlusClicked(object? sender, EventArgs? e)
	{
		if (MediaElement.Speed < 10)
		{
			MediaElement.Speed += 1;
		}
	}

	void OnVolumeMinusClicked(object? sender, EventArgs? e)
	{
		if (MediaElement.Volume >= 0)
		{
			if (MediaElement.Volume < .1)
			{
				MediaElement.Volume = 0;

				return;
			}

			MediaElement.Volume -= .1;
		}
	}

	void OnVolumePlusClicked(object? sender, EventArgs? e)
	{
		if (MediaElement.Volume < 1)
		{
			if (MediaElement.Volume > .9)
			{
				MediaElement.Volume = 1;

				return;
			}

			MediaElement.Volume += .1;
		}
	}

	void OnPlayClicked(object? sender, EventArgs? e)
	{
		MediaElement.Play();
	}

	void OnPauseClicked(object? sender, EventArgs? e)
	{
		MediaElement.Pause();
	}

	void OnStopClicked(object? sender, EventArgs? e)
	{
		MediaElement.Stop();
	}

	void OnMuteClicked(object? sender, EventArgs? e)
	{
		MediaElement.ShouldMute = !MediaElement.ShouldMute;
	}

	async void Slider_DragCompleted(object? sender, EventArgs? e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var newValue = ((Slider)sender).Value;
		await MediaElement.SeekTo(TimeSpan.FromSeconds(newValue), CancellationToken.None);

		MediaElement.Play();
	}

	void Slider_DragStarted(object? sender, EventArgs? e)
	{
		MediaElement.Pause();
	}

	async Task UpdateSourceStream(Stream stream)
	{
		try
		{
			await ReleaseSourceStream();
			sourceStream = new MemoryStream();
			await stream.CopyToAsync(sourceStream);
			sourceStream.Position = 0;

			MediaElement.Source = MediaSource.FromStream(sourceStream);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to stream video");
			await Toast.Make("Failed to stream video").Show();
		}
	}

	async void OnOpenVideoClicked(object? sender, EventArgs? e)
	{
		var pickerOptions = new MediaPickerOptions
		{
			Title = "Please select a video file",
			SelectionLimit = 1
		};

		var result = await MediaPicker.Default.PickVideosAsync(pickerOptions);

		if (result is null || result.Count == 0)
		{
			await Toast.Make("No video selected").Show();
			return;
		}

		var videoFile = result[0];

		await using var stream = await videoFile.OpenReadAsync();

		await UpdateSourceStream(stream);
	}

	async void OnRecordVideoClicked(object? sender, EventArgs? e)
	{
		FileResult? videoResult;

		try
		{
			videoResult = await MediaPicker.Default.CaptureVideoAsync(new MediaPickerOptions
			{
				Title = "Capture Video"
			});
		}
		catch (PermissionException ex)
		{
			// A permission that video capture needs was denied: the camera, the microphone (iOS, Mac Catalyst) or the storage (Android 12 and lower).
			// The message names it, for example "Camera permission was not granted: Denied"
			logger.LogWarning(ex, "Unable to record a video");
			await Toast.Make($"Unable to record a video. {ex.Message}").Show();
			return;
		}
		catch (FeatureNotSupportedException ex)
		{
			// The device has no camera that can record a video
			logger.LogWarning(ex, "Unable to record a video");
			await Toast.Make("Unable to record a video. Video capture is not supported on this device.").Show();
			return;
		}

		if (videoResult is null)
		{
			return;
		}

		await using var stream = await videoResult.OpenReadAsync();
		await UpdateSourceStream(stream);
	}

	async void ChangeAspectClicked(object? sender, EventArgs? e)
	{
		const string cancel = "Cancel";

		var resultAspect = await DisplayActionSheetAsync(
			"Choose aspect ratio",
			cancel,
			null,
			nameof(Aspect.AspectFit),
			nameof(Aspect.AspectFill),
			nameof(Aspect.Fill));

		if (resultAspect is null or cancel)
		{
			return;
		}

		if (!Enum.TryParse(typeof(Aspect), resultAspect, true, out var aspectEnum))
		{
			await DisplayAlertAsync("Error", "There was an error determining the selected aspect", "OK");

			return;
		}

		MediaElement.Aspect = (Aspect)aspectEnum;
	}

	async ValueTask ReleaseSourceStream()
	{
		if (sourceStream is null)
		{
			return;
		}

		try
		{
			await sourceStreamSemaphore.WaitAsync();
			await (sourceStream?.DisposeAsync() ?? ValueTask.CompletedTask);
			sourceStream = null;
		}
		finally
		{
			sourceStreamSemaphore.Release();
		}
	}
}