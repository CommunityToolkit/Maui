using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.MediaView;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaViewPage : BasePage<MediaViewViewModel>
{
	readonly ILogger logger;

	public MediaViewPage(MediaViewViewModel viewModel, ILogger<MediaViewPage> logger) : base(viewModel)
	{
		InitializeComponent();

		this.logger = logger;
	}

	void OnMediaOpened(object? sender, EventArgs e) => logger.LogInformation("Media opened.");

	void OnStateChanged(object? sender, MediaStateChangedEventArgs e)
	{
		logger.LogInformation("Media State Changed. Old State: {PreviousState}, New State: {NewState}", e.PreviousState, e.NewState);

		//if (e.NewState == MediaViewState.Playing)
		//{
		//	positionSlider.SetBinding(PositionSlider.DurationProperty,
		//		new Binding(nameof(mediaView.Duration),
		//		source: mediaView));

		//	positionSlider.SetBinding(PositionSlider.PositionProperty,
		//		new Binding(nameof(mediaView.Position),
		//		source: mediaView));
		//}
	}

	void OnMediaFailed(object? sender, MediaFailedEventArgs e) => logger.LogInformation("Media failed. Error: {ErrorMessage}", e.ErrorMessage);

	void OnMediaEnded(object? sender, EventArgs e) => logger.LogInformation("Media ended.");

	void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e) => logger.LogInformation("Position changed to {position}", e.Position);

	void OnSeekCompleted(object? sender, EventArgs e) => logger.LogInformation("Seek completed.");

	void OnResetClicked(object? sender, EventArgs e)
	{
		mediaView.Source = null;
	}

	void OnMp4OnlineSourceClicked(object? sender, EventArgs e)
	{
		mediaView.Source = MediaSource.FromUri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");
	}

	void OnHlsSourceClicked(object? sender, EventArgs e)
	{
		mediaView.Source = MediaSource.FromUri("https://wowza.peer5.com/live/smil:bbb_abr.smil/playlist.m3u8");
	}

	void OnResourceSourceClicked(object? sender, EventArgs e)
	{
		if (DeviceInfo.Platform == DevicePlatform.MacCatalyst
			|| DeviceInfo.Platform == DevicePlatform.iOS)
		{
			mediaView.Source = MediaSource.FromResource("AppleVideo.mp4");
		}
		else if (DeviceInfo.Platform == DevicePlatform.Android)
		{
			mediaView.Source = MediaSource.FromResource("AndroidVideo.mp4");
		}
		else if (DeviceInfo.Platform == DevicePlatform.WinUI)
		{
			mediaView.Source = MediaSource.FromResource("WindowsVideo.mp4");
		}
	}

	void OnSpeedMinusClicked(object? sender, EventArgs e)
	{
		if (mediaView.Speed >= 1)
		{
			mediaView.Speed -= 1;
		}
	}

	void OnSpeedPlusClicked(object? sender, EventArgs e)
	{
		if (mediaView.Speed < 10)
		{
			mediaView.Speed += 1;
		}
	}

	void OnVolumeMinusClicked(object? sender, EventArgs e)
	{
		if (mediaView.Volume >= 0)
		{
			if (mediaView.Volume < .1)
			{
				mediaView.Volume = 0;

				return;
			}

			mediaView.Volume -= .1;
		}
	}

	void OnVolumePlusClicked(object? sender, EventArgs e)
	{
		if (mediaView.Volume < 1)
		{
			if (mediaView.Volume > .9)
			{
				mediaView.Volume = 1;

				return;
			}

			mediaView.Volume += .1;
		}
	}

	void OnPlayClicked(object? sender, EventArgs e)
	{
		mediaView.Play();
	}

	void OnPauseClicked(object? sender, EventArgs e)
	{
		mediaView.Pause();
	}

	void OnStopClicked(object? sender, EventArgs e)
	{
		mediaView.Stop();
	}

	void BasePage_Unloaded(object? sender, EventArgs e)
	{
		// Stop and cleanup MediaView when we navigate away
		mediaView.Handler?.DisconnectHandler();
	}

	void Slider_DragCompleted(object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var newValue = ((Slider)sender).Value;
		mediaView.SeekTo(TimeSpan.FromSeconds(newValue));
	}
}