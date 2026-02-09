using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Constants;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaElementPage : BasePage<MediaElementViewModel>
{
	const string loadOnlineMp4 = "Load Online MP4";
	const string loadHls = "Load HTTP Live Stream (HLS)";
	const string loadLocalResource = "Load Local Resource";
	const string resetSource = "Reset Source to null";
	const string loadMusic = "Load Music";

	const string botImageUrl = "https://lh3.googleusercontent.com/pw/AP1GczNRrebWCJvfdIau1EbsyyYiwAfwHS0JXjbioXvHqEwYIIdCzuLodQCZmA57GADIo5iB3yMMx3t_vsefbfoHwSg0jfUjIXaI83xpiih6d-oT7qD_slR0VgNtfAwJhDBU09kS5V2T5ZML-WWZn8IrjD4J-g=w1792-h1024-s-no-gm";
	const string hlsStreamTestUrl = "https://mtoczko.github.io/hls-test-streams/test-gap/playlist.m3u8";
	const string hal9000AudioUrl = "https://github.com/prof3ssorSt3v3/media-sample-files/raw/master/hal-9000.mp3";


	readonly ILogger logger;
	readonly IDeviceInfo deviceInfo;
	readonly IFileSystem fileSystem;

	public MediaElementPage(MediaElementViewModel viewModel, IFileSystem fileSystem, IDeviceInfo deviceInfo, ILogger<MediaElementPage> logger) : base(viewModel)
	{
		InitializeComponent();

		this.logger = logger;
		this.deviceInfo = deviceInfo;
		this.fileSystem = fileSystem;
		MediaElement.PropertyChanged += MediaElement_PropertyChanged;
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

	protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
	{
		base.OnNavigatedFrom(args);
		MediaElement.Stop();
		MediaElement.Handler?.DisconnectHandler();
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

	async void Button_Clicked(object? sender, EventArgs? e)
	{
		if (string.IsNullOrWhiteSpace(CustomSourceEntry.Text))
		{
			await DisplayAlertAsync("Error Loading URL Source", "No value was found to load as a media source. " +
				"When you do enter a value, make sure it's a valid URL. No additional validation is done.",
				"OK");

			return;
		}

		MediaElement.Source = MediaSource.FromUri(CustomSourceEntry.Text);
	}

	async void ChangeSourceClicked(object? sender, EventArgs? e)
	{
		var result = await DisplayActionSheetAsync("Choose a source", "Cancel", null,
			loadOnlineMp4, loadHls, loadLocalResource, resetSource, loadMusic);

		MediaElement.Stop();
		MediaElement.Source = null;

		switch (result)
		{
			case loadOnlineMp4:
				MediaElement.MetadataTitle = "Big Buck Bunny";
				MediaElement.MetadataArtworkUrl = botImageUrl;
				MediaElement.MetadataArtist = "Big Buck Bunny Album";
				MediaElement.Source =
					MediaSource.FromUri(StreamingVideoUrls.BuckBunny);
				return;

			case loadHls:
				MediaElement.MetadataArtist = "HLS Album";
				MediaElement.MetadataArtworkUrl = botImageUrl;
				MediaElement.MetadataTitle = "HLS Title";
				MediaElement.Source = MediaSource.FromUri(hlsStreamTestUrl);
				return;

			case resetSource:
				MediaElement.MetadataArtworkUrl = string.Empty;
				MediaElement.MetadataTitle = string.Empty;
				MediaElement.MetadataArtist = string.Empty;
				MediaElement.Source = null;
				return;

			case loadLocalResource:
				MediaElement.MetadataArtworkUrl = botImageUrl;
				MediaElement.MetadataTitle = "Local Resource Title";
				MediaElement.MetadataArtist = "Local Resource Album";

				if (DeviceInfo.Platform == DevicePlatform.MacCatalyst
					|| DeviceInfo.Platform == DevicePlatform.iOS)
				{
					MediaElement.Source = MediaSource.FromResource("AppleVideo.mp4");
				}
				else if (DeviceInfo.Platform == DevicePlatform.Android)
				{
					MediaElement.Source = MediaSource.FromResource("AndroidVideo.mp4");
				}
				else if (DeviceInfo.Platform == DevicePlatform.WinUI)
				{
					MediaElement.Source = MediaSource.FromResource("WindowsVideo.mp4");
				}
				return;

			case loadMusic:
				MediaElement.MetadataTitle = "HAL 9000";
				MediaElement.MetadataArtist = "HAL 9000 Album";
				MediaElement.MetadataArtworkUrl = botImageUrl;
				MediaElement.Source = MediaSource.FromUri(hal9000AudioUrl);
				return;
		}
	}

	async void ChangeAspectClicked(object? sender, EventArgs? e)
	{
		const string cancel = "Cancel";

		var resultAspect = await DisplayActionSheetAsync(
			"Choose aspect ratio",
			cancel,
			null,
			Aspect.AspectFit.ToString(),
			Aspect.AspectFill.ToString(),
			Aspect.Fill.ToString());

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

	async void DisplayPopup(object? sender, EventArgs? e)
	{
		MediaElement.Pause();

		MediaSource source;

		if (deviceInfo.Platform == DevicePlatform.Android)
		{
			source = MediaSource.FromResource("AndroidVideo.mp4");
		}
		else if (deviceInfo.Platform == DevicePlatform.MacCatalyst
				 || deviceInfo.Platform == DevicePlatform.iOS
				 || deviceInfo.Platform == DevicePlatform.macOS)
		{
			source = MediaSource.FromResource("AppleVideo.mp4");
		}
		else
		{
			source = MediaSource.FromResource("WindowsVideo.mp4");
		}

		var popupMediaElement = new MediaElement
		{
			WidthRequest = 600,
			HeightRequest = 400,
			AndroidViewType = AndroidViewType.SurfaceView,
			Source = source,
			MetadataArtworkUrl = botImageUrl,
			ShouldAutoPlay = true,
			ShouldShowPlaybackControls = true,
		};

		await this.ShowPopupAsync(popupMediaElement);

		popupMediaElement.Stop();
		popupMediaElement.Source = null;
	}
}