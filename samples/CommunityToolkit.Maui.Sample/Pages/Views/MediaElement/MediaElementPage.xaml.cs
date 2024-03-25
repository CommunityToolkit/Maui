using System.ComponentModel;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaElementPage : BasePage<MediaElementViewModel>
{
	readonly ILogger logger;
	const string loadOnlineMp4 = "Load Online MP4";
	const string loadHls = "Load HTTP Live Stream (HLS)";
	const string loadLocalResource = "Load Local Resource";
	const string resetSource = "Reset Source to null";

	public MediaElementPage(MediaElementViewModel viewModel, ILogger<MediaElementPage> logger) : base(viewModel)
	{
		InitializeComponent();

		this.logger = logger;
		MediaElement.PropertyChanged += MediaElement_PropertyChanged;
	}

	void MediaElement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == MediaElement.DurationProperty.PropertyName)
		{
			logger.LogInformation("Duration: {newDuration}", MediaElement.Duration);
			PositionSlider.Maximum = MediaElement.Duration.TotalSeconds;
		}
	}

	void OnMediaOpened(object? sender, EventArgs e) => logger.LogInformation("Media opened.");

	void OnStateChanged(object? sender, MediaStateChangedEventArgs e) =>
		logger.LogInformation("Media State Changed. Old State: {PreviousState}, New State: {NewState}", e.PreviousState, e.NewState);

	void OnMediaFailed(object? sender, MediaFailedEventArgs e) => logger.LogInformation("Media failed. Error: {ErrorMessage}", e.ErrorMessage);

	void OnMediaEnded(object? sender, EventArgs e) => logger.LogInformation("Media ended.");

	void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e)
	{
		logger.LogInformation("Position changed to {position}", e.Position);
		PositionSlider.Value = e.Position.TotalSeconds;
	}

	void OnSeekCompleted(object? sender, EventArgs e) => logger.LogInformation("Seek completed.");

	void OnSpeedMinusClicked(object? sender, EventArgs e)
	{
		if (MediaElement.Speed >= 1)
		{
			MediaElement.Speed -= 1;
		}
	}

	void OnSpeedPlusClicked(object? sender, EventArgs e)
	{
		if (MediaElement.Speed < 10)
		{
			MediaElement.Speed += 1;
		}
	}

	void OnVolumeMinusClicked(object? sender, EventArgs e)
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

	void OnVolumePlusClicked(object? sender, EventArgs e)
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

	void OnPlayClicked(object? sender, EventArgs e)
	{
		MediaElement.Play();
	}

	void OnPauseClicked(object? sender, EventArgs e)
	{
		MediaElement.Pause();
	}

	void OnStopClicked(object? sender, EventArgs e)
	{
		MediaElement.Stop();
	}

	void OnMuteClicked(object? sender, EventArgs e)
	{
		MediaElement.ShouldMute = !MediaElement.ShouldMute;
	}

	protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
	{
		base.OnNavigatedFrom(args);
		MediaElement.Handler?.DisconnectHandler();
	}

	async void Slider_DragCompleted(object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var newValue = ((Slider)sender).Value;
		await MediaElement.SeekTo(TimeSpan.FromSeconds(newValue), CancellationToken.None);

		MediaElement.Play();
	}

	void Slider_DragStarted(object sender, EventArgs e)
	{
		MediaElement.Pause();
	}

	void Button_Clicked(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(CustomSourceEntry.Text))
		{
			DisplayAlert("Error Loading URL Source", "No value was found to load as a media source. " +
				"When you do enter a value, make sure it's a valid URL. No additional validation is done.",
				"OK");

			return;
		}

		MediaElement.Source = MediaSource.FromUri(CustomSourceEntry.Text);
	}

	async void ChangeSourceClicked(Object sender, EventArgs e)
	{
		var result = await DisplayActionSheet("Choose a source", "Cancel", null,
			loadOnlineMp4, loadHls, loadLocalResource, resetSource);

		switch (result)
		{
			case loadOnlineMp4:
				MediaElement.Source =
					MediaSource.FromUri(
						"https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");
				return;

			case loadHls:
				MediaElement.Source
					= MediaSource.FromUri(
						"https://mtoczko.github.io/hls-test-streams/test-gap/playlist.m3u8");
				return;

			case resetSource:
				MediaElement.Source = null;
				return;

			case loadLocalResource:
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
		}
	}

	async void ChangeAspectClicked(Object sender, EventArgs e)
	{
		var resultAspect = await DisplayActionSheet("Choose aspect ratio",
			"Cancel", null, Aspect.AspectFit.ToString(),
			Aspect.AspectFill.ToString(), Aspect.Fill.ToString());

		if (resultAspect.Equals("Cancel"))
		{
			return;
		}

		if (!Enum.TryParse(typeof(Aspect), resultAspect, true, out var aspectEnum)
			|| aspectEnum is null)
		{
			await DisplayAlert("Error", "There was an error determining the selected aspect",
				"OK");

			return;
		}

		MediaElement.Aspect = (Aspect)aspectEnum;
	}

	void DisplayPopup(object sender, EventArgs e)
	{
		MediaElement.Pause();
		MediaElement popupMediaElement = new MediaElement
		{
			Source = MediaSource.FromResource("AppleVideo.mp4"),
			HeightRequest = 600,
			WidthRequest = 600,
			ShouldAutoPlay = true,
			ShouldShowPlaybackControls = true,
		};
		var popup = new Popup
		{
			VerticalOptions = LayoutAlignment.Center,
			HorizontalOptions = LayoutAlignment.Center,
		};
		popup.Content = new StackLayout
		{
			Children =
			{
				popupMediaElement,
			}
		};

		this.ShowPopup(popup);
		popup.Closed += (s, e) =>
		{
			popupMediaElement.Stop();
			popupMediaElement.Handler?.DisconnectHandler();
		};
	}

	void MediaElementUnloaded(object sender, EventArgs e)
	{
		MediaElement.Handler?.DisconnectHandler();
	}
}