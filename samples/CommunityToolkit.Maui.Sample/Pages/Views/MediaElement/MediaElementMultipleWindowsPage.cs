using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Markup;
namespace CommunityToolkit.Maui.Sample.Pages.Views;

public class MediaElementMultipleWindowsPage : BasePage<MediaElementMultipleWindowsViewModel>
{
	const string buckBunnyMp4Url = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
	readonly Window secondWindow;

	public MediaElementMultipleWindowsPage(MediaElementMultipleWindowsViewModel viewModel) : base(viewModel)
	{
		secondWindow = new Window(new ContentPage
		{
			Content = new MediaElement
			{
				Source = buckBunnyMp4Url,
				ShouldAutoPlay = true
			}
		});

#if WINDOWS || MACCATALYST
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