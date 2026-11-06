using CommunityToolkit.Maui.Extensions;
using Microsoft.UI.Xaml.Media.Imaging;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WPanel = Microsoft.UI.Xaml.Controls.Panel;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class GravatarImageSourceTests
{
	// .NET MAUI sets the stream of a StreamImageSource on a BitmapImage through AsRandomAccessStream, which throws for a stream that cannot seek.
	[Fact]
	public async Task BitmapImageDecodesTheStreamOfAResponseWhoseBodyCannotSeek()
	{
		using var client = new HttpClient(new PictureHandler());
		await using var stream = await client.DownloadStreamAsync(pictureUri, CancellationToken.None);

		var decodedSize = await InvokeOnMainThreadAsync(async () =>
		{
			var bitmap = new BitmapImage();
			await bitmap.SetSourceAsync(stream.AsRandomAccessStream());

			return (bitmap.PixelWidth, bitmap.PixelHeight);
		});

		Assert.Equal((pictureSize, pictureSize), decodedSize);
	}

	static bool HasPicture(IPlatformViewHandler handler) => GetNativeImage(handler)?.Source is BitmapImage { PixelWidth: pictureSize, PixelHeight: pictureSize };

	static WImage? GetNativeImage(IPlatformViewHandler handler) => handler.PlatformView switch
	{
		WImage image => image,
		WButton button => button.Content switch
		{
			WImage buttonImage => buttonImage,
			WPanel panel => panel.Children.OfType<WImage>().FirstOrDefault(),
			_ => null,
		},
		_ => null,
	};
}