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

	private void Slider_DragCompleted(object sender, EventArgs e)
	{
		mediaElement.Speed = MainSlider.Value;
	}
}
