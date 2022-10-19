using System;
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

	void OnMediaOpened(object? sender, EventArgs e) => Console.WriteLine("Media opened.");

	void OnMediaFailed(object? sender, EventArgs e) => Console.WriteLine("Media failed.");

	void OnMediaEnded(object? sender, EventArgs e) => Console.WriteLine("Media ended.");

	void OnSeekCompleted(object? sender, EventArgs e) => Console.WriteLine("Seek completed.");

	void OnResetClicked(object? sender, EventArgs e) => mediaElement.Source = null;

	void OnMp4OnlineSourceClicked(object? sender, EventArgs e) => mediaElement.Source = MediaSource.FromUri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");

	void OnHlsSourceClicked(object? sender, EventArgs e) => mediaElement.Source = MediaSource.FromUri("https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8");

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