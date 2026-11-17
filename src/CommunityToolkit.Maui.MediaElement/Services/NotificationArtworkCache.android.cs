using Android.Graphics;

namespace CommunityToolkit.Maui.Media.Services;

sealed class NotificationArtworkCache
{
	// Bounds only the decode: notify() scales the large icon down to 48 dp itself (NotificationCompat to 320 dp before API 27), so a
	// full-size cover would only cost memory.
	internal const int MaximumArtworkDimension = 1024;

	byte[]? cachedBytes;
	Bitmap? cachedBitmap;

	public Bitmap? GetOrDecode(IList<byte>? artworkData)
	{
		if (artworkData is null || artworkData.Count == 0)
		{
			Clear();
			return null;
		}

		var bytes = artworkData.ToArray();
		if (cachedBytes is not null && cachedBytes.AsSpan().SequenceEqual(bytes))
		{
			return cachedBitmap;
		}

		Clear();
		cachedBytes = bytes;
		cachedBitmap = DecodeDownsampled(bytes);
		return cachedBitmap;
	}

	// Not Recycle(): the builder's large icon still references this bitmap until the next SetLargeIcon; Dispose drops only our JNI reference.
	public void Clear()
	{
		cachedBitmap?.Dispose();
		cachedBitmap = null;
		cachedBytes = null;
	}

	static Bitmap? DecodeDownsampled(byte[] bytes)
	{
		using var boundsOptions = new BitmapFactory.Options { InJustDecodeBounds = true };
		BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, boundsOptions);
		var largerSide = Math.Max(boundsOptions.OutWidth, boundsOptions.OutHeight);
		if (largerSide <= 0)
		{
			return null;
		}

		var sampleSize = 1;
		while (largerSide / sampleSize > MaximumArtworkDimension)
		{
			sampleSize *= 2;
		}

		using var decodeOptions = new BitmapFactory.Options { InSampleSize = sampleSize };
		return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, decodeOptions);
	}
}