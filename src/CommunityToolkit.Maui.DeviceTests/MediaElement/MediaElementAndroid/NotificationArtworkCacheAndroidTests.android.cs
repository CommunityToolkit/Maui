using Android.Graphics;
using CommunityToolkit.Maui.Media.Services;
using Color = Android.Graphics.Color;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementAndroid)]
public class NotificationArtworkCacheAndroidTests
{
	static byte[] CreateJpeg(int width, int height, Color? fill = null)
	{
		using var source = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!)!;
		source.EraseColor((fill ?? Color.Red).ToArgb());
		using var stream = new MemoryStream();
		Assert.True(source.Compress(Bitmap.CompressFormat.Jpeg!, 80, stream));
		return stream.ToArray();
	}

	[Fact]
	public void LargeArtworkIsDownsampledBelowTheCapKeepingMostOfIt()
	{
		var cache = new NotificationArtworkCache();

		var bitmap = cache.GetOrDecode(CreateJpeg(3000, 3000));

		Assert.NotNull(bitmap);
		var largerSide = Math.Max(bitmap.Width, bitmap.Height);
		Assert.True(largerSide <= NotificationArtworkCache.MaximumArtworkDimension, $"Larger side was {largerSide}.");
		Assert.True(largerSide > NotificationArtworkCache.MaximumArtworkDimension / 2, $"Larger side was {largerSide}.");
	}

	[Fact]
	public void NonSquareArtworkKeepsItsAspectRatio()
	{
		var cache = new NotificationArtworkCache();

		var bitmap = cache.GetOrDecode(CreateJpeg(4000, 2000));

		Assert.NotNull(bitmap);
		Assert.True(bitmap.Width <= NotificationArtworkCache.MaximumArtworkDimension);
		Assert.Equal(2.0, (double)bitmap.Width / bitmap.Height, 0.05);
	}

	[Fact]
	public void SmallArtworkIsNotUpscaled()
	{
		var cache = new NotificationArtworkCache();

		var bitmap = cache.GetOrDecode(CreateJpeg(300, 200));

		Assert.NotNull(bitmap);
		Assert.Equal(300, bitmap.Width);
		Assert.Equal(200, bitmap.Height);
	}

	[Fact]
	public void SameBytesReuseTheDecodedBitmap()
	{
		var cache = new NotificationArtworkCache();
		var bytes = CreateJpeg(1500, 1500);

		var first = cache.GetOrDecode(bytes);
		var second = cache.GetOrDecode(bytes.ToArray());

		Assert.NotNull(first);
		Assert.Same(first, second);
	}

	[Fact]
	public void DifferentBytesDecodeANewBitmapAndReleaseTheOldPeer()
	{
		var cache = new NotificationArtworkCache();

		var first = cache.GetOrDecode(CreateJpeg(400, 400, Color.Red));
		var second = cache.GetOrDecode(CreateJpeg(400, 400, Color.Blue));

		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.NotSame(first, second);
		Assert.Equal(IntPtr.Zero, first.Handle);
	}

	[Fact]
	public void NullAndEmptyArtworkGiveNoBitmapAndForgetTheCachedOne()
	{
		var cache = new NotificationArtworkCache();
		var bytes = CreateJpeg(400, 400);
		var first = cache.GetOrDecode(bytes);

		Assert.Null(cache.GetOrDecode(null));
		Assert.Null(cache.GetOrDecode([]));
		var again = cache.GetOrDecode(bytes);

		Assert.NotNull(first);
		Assert.NotNull(again);
		Assert.NotSame(first, again);
	}

	[Fact]
	public void ClearReleasesTheCachedBitmap()
	{
		var cache = new NotificationArtworkCache();
		var bitmap = cache.GetOrDecode(CreateJpeg(400, 400));

		cache.Clear();

		Assert.NotNull(bitmap);
		Assert.Equal(IntPtr.Zero, bitmap.Handle);
	}

	[Theory]
	[InlineData(1024, 1024)]
	[InlineData(2048, 1024)]
	public void ArtworkAtOrAnExactPowerOfTwoAboveTheCapDecodesToTheCap(int sourceSize, int expectedLargerSide)
	{
		var cache = new NotificationArtworkCache();

		var bitmap = cache.GetOrDecode(CreateJpeg(sourceSize, sourceSize));

		Assert.NotNull(bitmap);
		Assert.Equal(expectedLargerSide, Math.Max(bitmap.Width, bitmap.Height));
	}

	[Fact]
	public void UndecodableArtworkGivesNoBitmapWithoutThrowing()
	{
		var cache = new NotificationArtworkCache();
		byte[] notAnImage = [1, 2, 3, 4, 5];

		Assert.Null(cache.GetOrDecode(notAnImage));
		Assert.Null(cache.GetOrDecode(notAnImage.ToArray()));
	}
}