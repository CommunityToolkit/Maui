using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaElementTextureViewPage : BasePage<MediaElementTextureViewViewModel>
{
	readonly bool sHOW_LEFT_HALF_VIDEO = true;
	readonly bool sHOW_RIGHT_HALF_VIDEO = true;

	readonly List<MediaElement> mediaElements = [];
	readonly List<Border> borders = [];
	readonly List<AbsoluteLayout> absoluteLayouts = [];
	public MediaElementTextureViewPage(MediaElementTextureViewViewModel viewModel) : base(viewModel)
	{
		AbsoluteLayout abs = [];
		Content = abs;


		for (int i = 0; i < 2; i++)
		{
			Border border = new()
			{
				StrokeThickness = 0
			};
			borders.Add(border);

			AbsoluteLayout absolute = [];
			absoluteLayouts.Add(absolute);
			border.Content = absolute;

			MediaElement mediaElement = new();
			if (i == 0)
			{
				mediaElement.Source = MediaSource.FromResource("bunny.mp4");
			}
			else
			{
				mediaElement.Source = MediaSource.FromResource("woods.mp4");
			}
			mediaElement.MetadataTitle = "Texture View";
			mediaElement.MetadataArtist = "Community Toolkit";
			mediaElement.MetadataArtworkUrl = "https://lh3.googleusercontent.com/pw/AP1GczNRrebWCJvfdIau1EbsyyYiwAfwHS0JXjbioXvHqEwYIIdCzuLodQCZmA57GADIo5iB3yMMx3t_vsefbfoHwSg0jfUjIXaI83xpiih6d-oT7qD_slR0VgNtfAwJhDBU09kS5V2T5ZML-WWZn8IrjD4J-g=w1792-h1024-s-no-gm";
			mediaElement.ShouldAutoPlay = true;
			mediaElement.ShouldLoopPlayback = true;

			mediaElements.Add(mediaElement);
			absolute.Add(mediaElement);
		}


		//=============================
		//ADD VIDEOS TO HIERARCHY
		//=============================
		if (sHOW_LEFT_HALF_VIDEO)
		{
			abs.Add(borders[0]);
		}
		if (sHOW_RIGHT_HALF_VIDEO)
		{
			abs.Add(borders[1]);
		}


		Content.SizeChanged += delegate {
			for (int i = 0; i < borders.Count; i++)
			{
				borders[i].HeightRequest = Content.Height;
				borders[i].WidthRequest = Content.Width * 0.5;

				mediaElements[i].HeightRequest = Content.Height;
				mediaElements[i].WidthRequest = Content.Width;

				if (i == 1)
				{
					borders[i].TranslationX = 0.5 * Content.Width;
					mediaElements[i].TranslationX = -0.5 * Content.Width;
				}
			}
		};
	}
}