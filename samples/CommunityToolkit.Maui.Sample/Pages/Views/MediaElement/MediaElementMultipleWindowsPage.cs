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
	readonly Window secondWindow;
#endif

	public MediaElementMultipleWindowsPage(MediaElementMultipleWindowsViewModel viewModel) : base(viewModel)
	{
#if WINDOWS || MACCATALYST
		secondWindow = new Window(new ContentPage
		{
			Content = new MediaElement
			{
				Source = StreamingVideoUrls.ElephantsDream,
				ShouldAutoPlay = true
			}
		});

		Content = new MediaElement
		{
			Source = StreamingVideoUrls.BuckBunny,
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