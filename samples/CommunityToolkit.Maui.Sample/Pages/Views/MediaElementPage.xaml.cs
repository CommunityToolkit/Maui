using System.Diagnostics;
using CommunityToolkit.Maui.MediaElement;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaElementPage : BasePage<MediaElementViewModel>
{
	public MediaElementPage(MediaElementViewModel viewModel)
		: base(viewModel)
	{
		InitializeComponent();
	}

	void OnMediaOpened(object? sender, EventArgs e) => Debug.WriteLine("Media opened.");

	void OnStateChanged(object? sender, MediaStateChangedEventArgs e) => Debug.WriteLine($"Media State Changed. Old State: {e.PreviousState}, New State: {e.NewState}");

	void OnMediaFailed(object? sender, MediaFailedEventArgs e) => Debug.WriteLine($"Media failed. Error: {e.ErrorMessage}");

	void OnMediaEnded(object? sender, EventArgs e) => Debug.WriteLine("Media ended.");

	void OnSeekCompleted(object? sender, EventArgs e) => Debug.WriteLine("Seek completed.");

	void OnResetClicked(object? sender, EventArgs e) => mediaElement.Source = null;

	void OnMp4OnlineSourceClicked(object? sender, EventArgs e) => mediaElement.Source = MediaSource.FromUri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");

	void OnHlsSourceClicked(object? sender, EventArgs e) => mediaElement.Source = MediaSource.FromUri("https://devstreaming-cdn.apple.com/videos/streaming/examples/bipbop_4x3/gear1/prog_index.m3u8");

	void SpeedMinusClicked(System.Object sender, System.EventArgs e)
	{
		if (mediaElement.Speed >= 1)
		{
			mediaElement.Speed -= 1;
		}
	}

	void SpeedPlusClicked(System.Object sender, System.EventArgs e)
	{
		if (mediaElement.Speed < 10)
		{
			mediaElement.Speed += 1;
		}
	}

	void PlayClicked(System.Object sender, System.EventArgs e)
	{
		mediaElement.Play();
	}

	void PauseClicked(System.Object sender, System.EventArgs e)
	{
		mediaElement.Pause();
	}

	void StopClicked(System.Object sender, System.EventArgs e)
	{
		mediaElement.Stop();
	}
}