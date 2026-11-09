using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
#if WINDOWS || MACCATALYST
using CommunityToolkit.Maui.Sample.Constants;
using CommunityToolkit.Maui.Views;
#else
using CommunityToolkit.Maui.Markup;
#endif

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaElementMultipleWindowsPage : BasePage<MediaElementMultipleWindowsViewModel>
{
#if WINDOWS || MACCATALYST
	Window? secondWindow;
#endif

	public MediaElementMultipleWindowsPage(MediaElementMultipleWindowsViewModel viewModel) : base(viewModel)
	{
#if WINDOWS || MACCATALYST
		Content = new MediaElement
		{
			AndroidViewType= AndroidViewType.SurfaceView,
			Source = StreamingUrls.BuckBunny,
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
		// The page opens its second window once, however often it appears.
		if (secondWindow is not null)
		{
			return;
		}

		secondWindow = new Window(new ContentPage
		{
			Content = new MediaElement
			{
				AndroidViewType= AndroidViewType.SurfaceView,
				Source = StreamingUrls.Jellyfish,
				ShouldAutoPlay = true
			}
		});

		Application.Current?.OpenWindow(secondWindow);
#endif
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
#if WINDOWS || MACCATALYST
		// Otherwise every visit to the page leaves one more window open.
		if (secondWindow is null)
		{
			return;
		}

		if (secondWindow.Page is ContentPage { Content: MediaElement mediaElement })
		{
			mediaElement.Stop();
			mediaElement.Handler?.DisconnectHandler();
		}

		Application.Current?.CloseWindow(secondWindow);
		secondWindow = null;
#endif
	}
}