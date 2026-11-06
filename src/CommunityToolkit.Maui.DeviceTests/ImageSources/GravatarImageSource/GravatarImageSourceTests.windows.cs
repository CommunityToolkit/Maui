using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.ImageSources;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Media.Imaging;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WPanel = Microsoft.UI.Xaml.Controls.Panel;
using WRect = Windows.Foundation.Rect;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

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

	// A picture is requested at the size of its Button and the Button has no size request for Left and Right, so a picture that fills the Button's
	// width, like the one .NET MAUI draws for a picture decoded from a stream, is wider than the Button's text line is tall.
	[Theory]
	[InlineData(Button.ButtonContentLayout.ImagePosition.Left)]
	[InlineData(Button.ButtonContentLayout.ImagePosition.Right)]
	[InlineData(Button.ButtonContentLayout.ImagePosition.Top)]
	[InlineData(Button.ButtonContentLayout.ImagePosition.Bottom)]
	public async Task ButtonDoesNotStretchAGravatarPictureBeyondItsNaturalSize(Button.ButtonContentLayout.ImagePosition position)
	{
		SetupBuilder();
		using var client = new HttpClient(new PictureHandler());

		await RunWithGravatarButtonAsync(client, position, async (button, platformButton, image) =>
		{
			var scale = platformButton.XamlRoot.RasterizationScale;
			var naturalSize = pictureSize / scale;

			AssertNotStretched();
			await InvokeOnMainThreadAsync(platformButton.UpdateLayout);
			AssertNotStretched();

			void AssertNotStretched()
			{
				var message = $"The {position} picture is drawn {image.ActualWidth}x{image.ActualHeight} DIP, its natural size is {naturalSize} DIP (scale {scale}, MaxHeight {image.MaxHeight}, Button.Height {button.Height}).";
				Assert.True(image.ActualWidth > 0 && image.ActualHeight > 0, message);
				Assert.True(image.ActualWidth <= naturalSize + 0.5 && image.ActualHeight <= naturalSize + 0.5, message);
			}
		}, checkGrowth: false, pictureBeforeFirstLayout: false);
	}

	// The picture is requested at the size of its Button, so a Button that grew for its picture would request a larger one each time.
	[Theory]
	[InlineData(Button.ButtonContentLayout.ImagePosition.Left, false)]
	[InlineData(Button.ButtonContentLayout.ImagePosition.Right, false)]
	[InlineData(Button.ButtonContentLayout.ImagePosition.Top, false)]
	[InlineData(Button.ButtonContentLayout.ImagePosition.Bottom, false)]
	[InlineData(Button.ButtonContentLayout.ImagePosition.Left, true)]
	[InlineData(Button.ButtonContentLayout.ImagePosition.Top, true)]
	public async Task ButtonDrawsALargeGravatarPictureWholeInsideItself(Button.ButtonContentLayout.ImagePosition position, bool pictureBeforeFirstLayout)
	{
		SetupBuilder();
		using var client = new HttpClient(new PictureHandler(largeRedPngBase64));

		await RunWithGravatarButtonAsync(client, position, async (button, platformButton, image) =>
		{
			AssertInside();
			await InvokeOnMainThreadAsync(platformButton.UpdateLayout);
			AssertInside();

			void AssertInside()
			{
				var picture = image.TransformToVisual(platformButton).TransformBounds(new WRect(0, 0, image.ActualWidth, image.ActualHeight));
				var message = $"The {position} picture is at {picture} but the Button is {button.Width}x{button.Height} DIP (MaxHeight {image.MaxHeight}, Height {image.Height}, picture before the first layout {pictureBeforeFirstLayout}).";

				Assert.True(picture.Height > 0, message);
				Assert.True(picture.Left >= -0.5 && picture.Top >= -0.5 && picture.Right <= button.Width + 0.5 && picture.Bottom <= button.Height + 0.5, message);

				var textBlock = Assert.IsType<WTextBlock>(platformButton.GetContent<WTextBlock>());
				Assert.True(textBlock.ActualWidth > 0 && textBlock.ActualHeight > 0, $"{message} The text is not drawn.");

				var text = textBlock.TransformToVisual(platformButton).TransformBounds(new WRect(0, 0, textBlock.ActualWidth, textBlock.ActualHeight));
				Assert.True(text.Left >= -0.5 && text.Top >= -0.5 && text.Right <= button.Width + 0.5 && text.Bottom <= button.Height + 0.5, $"{message} The text is at {text}.");

				var overlapWidth = Math.Min(picture.Right, text.Right) - Math.Max(picture.Left, text.Left);
				var overlapHeight = Math.Min(picture.Bottom, text.Bottom) - Math.Max(picture.Top, text.Top);

				Assert.False(overlapWidth > 0.5 && overlapHeight > 0.5, $"{message} It covers the text at {text}.");
			}
		}, checkGrowth: true, pictureBeforeFirstLayout);
	}

	static async Task RunWithGravatarButtonAsync(HttpClient client, Button.ButtonContentLayout.ImagePosition position, Func<Button, WButton, WImage, Task> assertAfterReload, bool checkGrowth, bool pictureBeforeFirstLayout)
	{
		var source = new GravatarImageSource { Email = "a@example.org" };
		var sizedRequests = 0;
		var firstLayoutDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		// The Sample's picture comes from the network after the Button's first layout: an instant answer would let .NET MAUI measure the Button with
		// its picture.
		source.Stream = async token =>
		{
			await firstLayoutDone.Task.WaitAsync(token);

			if (source.Uri.Query.Contains("s=", StringComparison.Ordinal))
			{
				Interlocked.Increment(ref sizedRequests);
			}

			return await client.DownloadStreamAsync(pictureUri, token);
		};

		// The Sample's layout: a Button without a size request in a layout of fixed width. Above or below its text, a picture needs room, which a
		// HeightRequest gives.
		var button = new Button
		{
			Text = "Button",
			ImageSource = source,
			ContentLayout = new Button.ButtonContentLayout(position, 10),
		};

		var reference = new Button
		{
			Text = "Button",
			ContentLayout = new Button.ButtonContentLayout(position, 10),
		};

		if (position is Button.ButtonContentLayout.ImagePosition.Top or Button.ButtonContentLayout.ImagePosition.Bottom)
		{
			button.HeightRequest = 120;
			reference.HeightRequest = 120;
		}

		var layout = new VerticalStackLayout { WidthRequest = 300, Children = { reference } };

		if (!pictureBeforeFirstLayout)
		{
			layout.Children.Add(button);
		}

		await AttachAndRun(layout, async handler =>
		{
			try
			{
				if (pictureBeforeFirstLayout)
				{
					await InvokeOnMainThreadAsync(() => button.ToHandler(MauiContext));
					firstLayoutDone.SetResult();

					await AssertEventually(() => GetNativeImage(Assert.IsAssignableFrom<IPlatformViewHandler>(button.Handler))?.Source is BitmapSource { PixelHeight: > 0 }, timeout: pictureTimeout, message: "The Button never showed a picture before its first layout.");
					await InvokeOnMainThreadAsync(() => layout.Children.Add(button));
				}

				await layout.OnLoadedAsync();

				// On Windows, a view that is not loaded cannot be measured by InitializeViewHandler, and no MAUI parent lays this layout out later.
				if (!await AssertHelpers.Wait(() => button.Width > 0 && button.Height > 0, timeout: pictureTimeout))
				{
					var size = ((IView)layout).Measure(layout.WidthRequest, double.PositiveInfinity);
					((IView)layout).Arrange(new Rect(0, 0, size.Width, size.Height));
				}

				await AssertEventually(() => button.Width > 0 && button.Height > 0 && reference.Height > 0, timeout: pictureTimeout, message: "The Button was never laid out.");

				var platformButton = Assert.IsType<WButton>(button.Handler?.PlatformView);
				var image = GetNativeImage((IPlatformViewHandler)button.Handler);
				Assert.NotNull(image);

				if (!pictureBeforeFirstLayout)
				{
					Assert.Null(image.Source);
					firstLayoutDone.SetResult();
				}

				await AssertEventually(() => image.Source is BitmapSource { PixelHeight: > 0 }, timeout: pictureTimeout, message: "The Button never showed a picture.");
				var firstPicture = image.Source;

				// The first picture is the default one, requested before the Button has a size: the picture of the Button's size comes with a second request.
				await AssertEventually(() => Volatile.Read(ref sizedRequests) > 0 && image.Source is BitmapSource { PixelHeight: > 0 } latest && !ReferenceEquals(latest, firstPicture), timeout: pictureTimeout * 2, message: $"The Button never showed the picture of its size. The Uri of the source is {source.Uri}.");

				await assertAfterReload(button, platformButton, image);

				if (checkGrowth)
				{
					Assert.True(Math.Abs(button.Height - reference.Height) <= 0.5, $"The Button is {button.Height} DIP tall, the same Button without a picture {reference.Height} DIP.");
				}
			}
			finally
			{
				firstLayoutDone.TrySetResult();

				// Once its Button has a size, the source raises SourceChanged 737 ms later, after this test has ended and its MauiApp is disposed: the
				// handler would then load the source with disposed services and crash the app.
				button.ImageSource = null;
			}
		});
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