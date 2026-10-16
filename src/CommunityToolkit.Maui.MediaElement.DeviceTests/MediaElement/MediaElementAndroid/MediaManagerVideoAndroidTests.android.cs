using Android.Views;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// What the video of a MediaElement looks like on Android: the surface of the PlayerView that ExoPlayer renders into (TextureView shows
// the frames in the view hierarchy, so its bitmap can be read) and how the Aspect resizes it (PlatformUpdateAspect sets the resize
// mode of the AspectRatioFrameLayout that holds the surface). The video is the 64x48 H.264 test pattern (4:3, many colors).
[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerVideoAndroidTests : MediaElementAndroidTestBase
{
	[Fact]
	public async Task TextureViewShowsTheFramesOfThePlayingVideo()
	{
		SkipWhereTheVideoDecoderIsUnreliable();
		SetupProbeBuilder();
		var path = await CopyAssetToCacheAsync(videoAsset);
		var mediaElement = await CreateMediaElementAsync(() => new MediaElement { AndroidViewType = AndroidViewType.TextureView, WidthRequest = 200, HeightRequest = 100 }, element =>
		{
			element.ShouldAutoPlay = true;
			element.ShouldLoopPlayback = true;
			element.Source = MediaSource.FromFile(path);
		});

		await HostAsync(mediaElement, async _ =>
		{
			var textureView = Assert.IsType<TextureView>(GetPlayerView(mediaElement).VideoSurfaceView);

			// The first frames are black while the decoder starts: wait for the picture, which has many colors.
			await WaitUntilAsync(() => CountColors(textureView) > 50, "The TextureView never showed the picture of the video.", videoWaitTimeout);
		});
	}

	[Theory]
	[InlineData(Aspect.AspectFit)]
	[InlineData(Aspect.Center)]
	[InlineData(Aspect.Fill)]
	[InlineData(Aspect.AspectFill)]
	public async Task AspectResizesTheVideoInsideThePlayerView(Aspect aspect)
	{
		SkipWhereTheVideoDecoderIsUnreliable();
		SetupProbeBuilder();
		var path = await CopyAssetToCacheAsync(videoAsset);
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.Aspect = aspect;
			element.Source = MediaSource.FromFile(path);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder, videoWaitTimeout);
			await WaitUntilVideoSizeIsKnownAsync(mediaElement, recorder);
			var playerView = GetPlayerView(mediaElement);
			var contentFrame = Assert.IsType<AspectRatioFrameLayout>(playerView.VideoSurfaceView!.Parent);

			await WaitUntilAsync(() => IsSizedFor(aspect, playerView, contentFrame), $"The video of the {aspect} player view is {contentFrame.Width}x{contentFrame.Height} in {playerView.Width}x{playerView.Height}.");
		});
	}

	// The video is 4:3. Fit and Center keep it inside the player view, AspectFill covers the player view, Fill stretches it to the player view.
	static bool IsSizedFor(Aspect aspect, PlayerView playerView, AspectRatioFrameLayout contentFrame)
	{
		const double videoRatio = 64.0 / 48;
		const double tolerance = 2;
		double containerWidth = playerView.Width, containerHeight = playerView.Height;
		if (containerWidth <= 0 || containerHeight <= 0)
		{
			return false;
		}

		var containerIsWider = containerWidth / containerHeight > videoRatio;
		var (expectedWidth, expectedHeight) = aspect switch
		{
			Aspect.Fill => (containerWidth, containerHeight),
			Aspect.AspectFill => containerIsWider ? (containerWidth, containerWidth / videoRatio) : (containerHeight * videoRatio, containerHeight),
			_ => containerIsWider ? (containerHeight * videoRatio, containerHeight) : (containerWidth, containerWidth / videoRatio),
		};

		return Math.Abs(contentFrame.Width - expectedWidth) <= tolerance && Math.Abs(contentFrame.Height - expectedHeight) <= tolerance;
	}

	static int CountColors(TextureView textureView)
	{
		using var bitmap = textureView.Bitmap;
		if (bitmap is null)
		{
			return 0;
		}

		var colors = new HashSet<int>();
		for (var x = 0; x < bitmap.Width; x += 8)
		{
			for (var y = 0; y < bitmap.Height; y += 8)
			{
				colors.Add(bitmap.GetPixel(x, y));
			}
		}

		return colors.Count;
	}
}