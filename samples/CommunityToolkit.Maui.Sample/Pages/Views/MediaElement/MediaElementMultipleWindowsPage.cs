using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Constants;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaElementMultipleWindowsPage : BasePage<MediaElementMultipleWindowsViewModel>
{
	const string buckBunnyMp4Url = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
	const string elephantsDreamMp4Url = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4";
	readonly Window? secondWindow;


	public MediaElementMultipleWindowsPage(MediaElementMultipleWindowsViewModel viewModel) : base(viewModel)
	{
		secondWindow = new Window(new ContentPage
		{
			Content = new MediaElement
			{
				AndroidViewType= AndroidViewType.SurfaceView,
				Source = StreamingVideoUrls.ElephantsDream,
				ShouldAutoPlay = true
			}
		});

		Content = new MediaElement
		{
			AndroidViewType= AndroidViewType.SurfaceView,
			Source = StreamingVideoUrls.BuckBunny,
			ShouldAutoPlay = true
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if(secondWindow is null)
		{
			return;
		}
		Application.Current?.OpenWindow(secondWindow);
	}
}