using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.MediaPlayer;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaPlayerPage : BasePage<MediaPlayerViewModel>
{
	readonly ILogger logger;

	public MediaPlayerPage(MediaPlayerViewModel viewModel, ILogger<MediaPlayerPage> logger) : base(viewModel)
	{
		InitializeComponent();

		this.logger = logger;
	}

	void OnMediaOpened(object? sender, EventArgs e) => logger.LogInformation("Media opened.");

	void OnStateChanged(object? sender, MediaStateChangedEventArgs e)
	{
		logger.LogInformation("Media State Changed. Old State: {PreviousState}, New State: {NewState}", e.PreviousState, e.NewState);

		//if (e.NewState == MediaPlayerState.Playing)
		//{
		//	positionSlider.SetBinding(PositionSlider.DurationProperty,
		//		new Binding(nameof(mediaPlayer.Duration),
		//		source: mediaPlayer));

		//	positionSlider.SetBinding(PositionSlider.PositionProperty,
		//		new Binding(nameof(mediaPlayer.Position),
		//		source: mediaPlayer));
		//}
	}

	void OnMediaFailed(object? sender, MediaFailedEventArgs e) => logger.LogInformation("Media failed. Error: {ErrorMessage}", e.ErrorMessage);

	void OnMediaEnded(object? sender, EventArgs e) => logger.LogInformation("Media ended.");

	void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e) => logger.LogInformation("Position changed to {position}", e.Position);

	void OnSeekCompleted(object? sender, EventArgs e) => logger.LogInformation("Seek completed.");

	void OnResetClicked(object? sender, EventArgs e)
	{
		mediaPlayer.Source = null;
	}

	void OnMp4OnlineSourceClicked(object? sender, EventArgs e)
	{
		mediaPlayer.Source = MediaSource.FromUri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");
	}

	void OnHlsSourceClicked(object? sender, EventArgs e)
	{
		mediaPlayer.Source = MediaSource.FromUri("https://wowza.peer5.com/live/smil:bbb_abr.smil/playlist.m3u8");
	}

	void OnResourceSourceClicked(object? sender, EventArgs e)
	{
		if (DeviceInfo.Platform == DevicePlatform.MacCatalyst
			|| DeviceInfo.Platform == DevicePlatform.iOS)
		{
			mediaPlayer.Source = MediaSource.FromResource("AppleVideo.mp4");
		}
		else if (DeviceInfo.Platform == DevicePlatform.Android)
		{
			mediaPlayer.Source = MediaSource.FromResource("AndroidVideo.mp4");
		}
		else if (DeviceInfo.Platform == DevicePlatform.WinUI)
		{
			mediaPlayer.Source = MediaSource.FromResource("WindowsVideo.mp4");
		}
	}

	void OnSpeedMinusClicked(object? sender, EventArgs e)
	{
		if (mediaPlayer.Speed >= 1)
		{
			mediaPlayer.Speed -= 1;
		}
	}

	void OnSpeedPlusClicked(object? sender, EventArgs e)
	{
		if (mediaPlayer.Speed < 10)
		{
			mediaPlayer.Speed += 1;
		}
	}

	void OnVolumeMinusClicked(object? sender, EventArgs e)
	{
		if (mediaPlayer.Volume >= 0)
		{
			if (mediaPlayer.Volume < .1)
			{
				mediaPlayer.Volume = 0;

				return;
			}

			mediaPlayer.Volume -= .1;
		}
	}

	void OnVolumePlusClicked(object? sender, EventArgs e)
	{
		if (mediaPlayer.Volume < 1)
		{
			if (mediaPlayer.Volume > .9)
			{
				mediaPlayer.Volume = 1;

				return;
			}

			mediaPlayer.Volume += .1;
		}
	}

	void OnPlayClicked(object? sender, EventArgs e)
	{
		mediaPlayer.Play();
	}

	void OnPauseClicked(object? sender, EventArgs e)
	{
		mediaPlayer.Pause();
	}

	void OnStopClicked(object? sender, EventArgs e)
	{
		mediaPlayer.Stop();
	}

	void BasePage_Unloaded(object? sender, EventArgs e)
	{
		// Stop and cleanup MediaPlayer when we navigate away
		mediaPlayer.Handler?.DisconnectHandler();
	}

	void Slider_DragCompleted(object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var newValue = ((Slider)sender).Value;
		mediaPlayer.SeekTo(TimeSpan.FromSeconds(newValue));
		mediaPlayer.Play();
	}

	void Slider_DragStarted(object sender, EventArgs e)
	{
		mediaPlayer.Pause();
    }
}