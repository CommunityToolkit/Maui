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
		MediaElement.Handler?.DisconnectHandler();
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

		await using var networkStream = await httpClient.GetStreamAsync(StreamingUrls.BuckBunny);
		await UpdateSourceStream(networkStream);
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
		var videoResult = await MediaPicker.Default.CaptureVideoAsync(new MediaPickerOptions
		{
			Title = "Capture Video"
		});

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