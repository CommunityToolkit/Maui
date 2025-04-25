using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.Constants;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaElementTextureViewPage : BasePage<MediaElementTextureViewViewModel>
{
	public MediaElementTextureViewPage(MediaElementTextureViewViewModel viewModel) : base(viewModel)
	{
		var width = 400;
		var height = 150;
		Content = new AbsoluteLayout {
			new Border
			{
				HorizontalOptions = LayoutOptions.Center,
				StrokeThickness = 0,
				WidthRequest = width,
				HeightRequest = height,
				TranslationX = -width * 0.5,
				Content = new MediaElement
				{
					WidthRequest = width,
					TranslationX = width * 0.5,
					AndroidViewType = AndroidViewType.TextureView,
					Source = StreamingVideoUrls.BuckBunny,
					ShouldAutoPlay = true,
					MetadataArtist = "Community Toolkit",
					MetadataTitle = "Texture View",
					MetadataArtworkUrl = "https://lh3.googleusercontent.com/pw/AP1GczNRrebWCJvfdIau1EbsyyYiwAfwHS0JXjbioXvHqEwYIIdCzuLodQCZmA57GADIo5iB3yMMx3t_vsefbfoHwSg0jfUjIXaI83xpiih6d-oT7qD_slR0VgNtfAwJhDBU09kS5V2T5ZML-WWZn8IrjD4J-g=w1792-h1024-s-no-gm",
				}
			},
			new Border
			{
				HorizontalOptions = LayoutOptions.Center,
				StrokeThickness = 0,
				WidthRequest = width,
				HeightRequest = height,
				TranslationX = width * 0.5,
				Content = new MediaElement
				{
					WidthRequest = width,
					TranslationX = -width * 0.5,
					AndroidViewType = AndroidViewType.TextureView,
					Source = StreamingVideoUrls.ElephantsDream,
					ShouldAutoPlay = true,
					MetadataArtist = "Community Toolkit",
					MetadataTitle = "Texture View",
					MetadataArtworkUrl = "https://lh3.googleusercontent.com/pw/AP1GczNRrebWCJvfdIau1EbsyyYiwAfwHS0JXjbioXvHqEwYIIdCzuLodQCZmA57GADIo5iB3yMMx3t_vsefbfoHwSg0jfUjIXaI83xpiih6d-oT7qD_slR0VgNtfAwJhDBU09kS5V2T5ZML-WWZn8IrjD4J-g=w1792-h1024-s-no-gm",
				}
			}
		};
	}
}