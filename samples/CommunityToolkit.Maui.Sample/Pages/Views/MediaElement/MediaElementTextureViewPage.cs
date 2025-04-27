using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Constants;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MediaElementTextureViewPage : BasePage<MediaElementTextureViewViewModel>
{
	const int mediaElementHeight = 200;

	public MediaElementTextureViewPage(MediaElementTextureViewViewModel viewModel) : base(viewModel)
	{
		Content = new Grid
		{
			RowSpacing = 24,
			RowDefinitions = Rows.Define(
				(Row.DescriptionLabel, 64),
				(Row.ExplanationLabel, 64),
				(Row.Video, mediaElementHeight * 1.75)), // Make the Video Row height slightly less than the height of two MediaElements to ensure overlap

			Children =
			{
				new Label()
					.Row(Row.DescriptionLabel)
					.Text("This page demonstrates two overlaying MediaElements using TextureView on Android. The two videos below have the same height.")
					.Font(size: 16),

				new Label()
					.Row(Row.ExplanationLabel)
					.Text("It is not possible to have overlapping views using SurfaceView on Android; this is only possible using TextureView on Android.")
					.Font(size: 16),

				new TextureViewMediaElementInBorder(StreamingVideoUrls.BuckBunny, "https://lh3.googleusercontent.com/pw/AP1GczNRrebWCJvfdIau1EbsyyYiwAfwHS0JXjbioXvHqEwYIIdCzuLodQCZmA57GADIo5iB3yMMx3t_vsefbfoHwSg0jfUjIXaI83xpiih6d-oT7qD_slR0VgNtfAwJhDBU09kS5V2T5ZML-WWZn8IrjD4J-g=w1792-h1024-s-no-gm")
					.Row(Row.Video)
					.BackgroundColor(Colors.LightBlue)
					.Top(),

				new TextureViewMediaElementInBorder(StreamingVideoUrls.ElephantsDream,"https://lh3.googleusercontent.com/pw/AP1GczNRrebWCJvfdIau1EbsyyYiwAfwHS0JXjbioXvHqEwYIIdCzuLodQCZmA57GADIo5iB3yMMx3t_vsefbfoHwSg0jfUjIXaI83xpiih6d-oT7qD_slR0VgNtfAwJhDBU09kS5V2T5ZML-WWZn8IrjD4J-g=w1792-h1024-s-no-gm")
					.Row(Row.Video)
					.BackgroundColor(Colors.LightCoral)
					.Bottom(),
			}
		};
	}

	enum Row { DescriptionLabel, ExplanationLabel, Video }

	sealed class TextureViewMediaElementInBorder : Border
	{
		public TextureViewMediaElementInBorder(in string urlSource, in string metaDataArtworkUrl)
		{
			Stroke = Colors.Transparent;
			StrokeThickness = 0;
			Padding = 0;
			HeightRequest = mediaElementHeight;
			HorizontalOptions = LayoutOptions.Center;

			Content = new MediaElement
			{
				AndroidViewType = AndroidViewType.TextureView,
				Source = urlSource,
				ShouldAutoPlay = true,
				MetadataArtist = "Community Toolkit",
				MetadataTitle = "Texture View",
				MetadataArtworkUrl = metaDataArtworkUrl,
				Margin = 0,
				BackgroundColor = Colors.Transparent
			}.Center();
		}
	}
}