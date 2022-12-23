using CommunityToolkit.Maui.MediaElement;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaElementPage : BasePage<MediaElementViewModel>
{
	readonly ILogger logger;

	public (double, double, double) MediaElementPosition { get; set; }

	public MediaElementPage(MediaElementViewModel viewModel, ILogger<MediaElementPage> logger)
		: base(viewModel)
	{
		InitializeComponent();

		this.logger = logger;
	}

	void OnMediaOpened(object? sender, EventArgs e) => logger.LogInformation("Media opened.");

	void OnStateChanged(object? sender, MediaStateChangedEventArgs e) => logger.LogInformation("Media State Changed. Old State: {PreviousState}, New State: {NewState}", e.PreviousState, e.NewState);

	void OnMediaFailed(object? sender, MediaFailedEventArgs e) => logger.LogInformation("Media failed. Error: {ErrorMessage}", e.ErrorMessage);

	void OnMediaEnded(object? sender, EventArgs e) => logger.LogInformation("Media ended.");

	void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e) => logger.LogInformation("Position changed to {position}", e.Position);

	void OnSeekCompleted(object? sender, EventArgs e) => logger.LogInformation("Seek completed.");

	void OnResetClicked(object? sender, EventArgs e) => mediaElement.Source = null;

	void OnMp4OnlineSourceClicked(object? sender, EventArgs e) => mediaElement.Source = MediaSource.FromUri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");

	void OnHlsSourceClicked(object? sender, EventArgs e) => mediaElement.Source = MediaSource.FromUri("https://wowza.peer5.com/live/smil:bbb_abr.smil/playlist.m3u8");

	void OnResourceSourceClicked(object? sender, EventArgs e)
	{
		if (DeviceInfo.Platform == DevicePlatform.MacCatalyst
			|| DeviceInfo.Platform == DevicePlatform.iOS)
		{
			mediaElement.Source = MediaSource.FromResource("AppleVideo.mp4");
		}
		else if(DeviceInfo.Platform == DevicePlatform.Android)
		{
			mediaElement.Source = MediaSource.FromResource("AndroidVideo.mp4");
		}
		else if (DeviceInfo.Platform == DevicePlatform.WinUI)
		{
			mediaElement.Source = MediaSource.FromResource("WindowsVideo.mp4");
		}
	}

	void SpeedMinusClicked(object sender, EventArgs e)
	{
		if (mediaElement.Speed >= 1)
		{
			mediaElement.Speed -= 1;
		}
	}

	void SpeedPlusClicked(object sender, EventArgs e)
	{
		if (mediaElement.Speed < 10)
		{
			mediaElement.Speed += 1;
		}
	}

	void VolumeMinusClicked(object sender, EventArgs e)
	{
		if (mediaElement.Volume >= 0)
		{
			if (mediaElement.Volume < .1)
			{
				mediaElement.Volume = 0;

				return;
			}

			mediaElement.Volume -= .1;
		}
	}

	void VolumePlusClicked(object sender, EventArgs e)
	{
		if (mediaElement.Volume < 1)
		{
			if (mediaElement.Volume > .9)
			{
				mediaElement.Volume = 1;

				return;
			}

			mediaElement.Volume += .1;
		}
	}

	void PlayClicked(object sender, EventArgs e)
	{
		mediaElement.Play();
	}

	void PauseClicked(object sender, EventArgs e)
	{
		mediaElement.Pause();
	}

	void StopClicked(object sender, EventArgs e)
	{
		mediaElement.Stop();
	}

	void BasePage_Unloaded(object sender, EventArgs e)
	{
		// Stop and cleanup MediaElement when we navigate away
		mediaElement.Handler?.DisconnectHandler();
	}

	void Slider_DragCompleted(object sender, EventArgs e)
	{
		var newValue = ((Slider)sender).Value;
		mediaElement.SeekTo(TimeSpan.FromSeconds(newValue));
	}
}