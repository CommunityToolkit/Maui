using CommunityToolkit.Maui.Sample.ViewModels.Views;
#if WINDOWS || MACCATALYST
using CommunityToolkit.Maui.Views;
#else
using CommunityToolkit.Maui.Markup;
#endif

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public class MediaElementMultipleWindowsPage : BasePage<MediaElementMultipleWindowsViewModel>
{
	const string buckBunnyMp4Url = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
#if WINDOWS || MACCATALYST
	const string elephantsDreamMp4Url = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4";
	readonly Window secondWindow;
#endif

	public MediaElementMultipleWindowsPage(MediaElementMultipleWindowsViewModel viewModel) : base(viewModel)
	{
#if WINDOWS || MACCATALYST
		secondWindow = new Window(new ContentPage
		{
			Content = new MediaElement
			{
				Source = elephantsDreamMp4Url,
				ShouldAutoPlay = true
			}
		});

		Content = new MediaElement
		{
			Source = buckBunnyMp4Url,
			ShouldAutoPlay = true
		};
#else
		Content = new Label()
			.Text("This sample is only testable on MacCatalyst and Windows")
			.TextCenter();
#endif
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
#if WINDOWS || MACCATALYST
		Application.Current?.OpenWindow(secondWindow);
#endif
	}
}