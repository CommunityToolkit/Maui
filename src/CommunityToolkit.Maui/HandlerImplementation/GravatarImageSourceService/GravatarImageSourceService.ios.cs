using CommunityToolkit.Maui.Core;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace CommunityToolkit.Maui.Views;

public partial class GravatarImageSourceService
{
	internal string cacheDirectory = Path.Combine(FileSystem.CacheDirectory, "com.microsoft.maui", "MauiUriImages");

	/// <summary>Get image source.</summary>
	/// <param name="imageSource">Image source</param>
	/// <param name="scale">Scale.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Image source service result.</returns>
	public override Task<IImageSourceServiceResult<UIImage>?> GetImageAsync(IImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default) => GetImageAsync((IGravatarImageSource)imageSource, scale, cancellationToken);

	/// <summary>Get image source.</summary>
	/// <param name="imageSource">Image source</param>
	/// <param name="scale">Scale.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Image source service result.</returns>
	public async Task<IImageSourceServiceResult<UIImage>?> GetImageAsync(IGravatarImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default)
	{
		if (imageSource.IsEmpty || imageSource.Uri is null)
		{
			return null;
		}

		try
		{
			var hash = Crc64.ComputeHashString(imageSource.Uri.OriginalString);
			var pathToImageCache = cacheDirectory + hash + ".png";
			NSData? imageData;
			if (imageSource.CachingEnabled && IsImageCached(pathToImageCache))
			{
				imageData = GetCachedImage(pathToImageCache);
			}
			else
			{
				// TODO: use a real caching library with the URI
				if (imageSource is not IStreamImageSource streamImageSource)
				{
					return null;
				}

				using Stream stream = await streamImageSource.GetStreamAsync(cancellationToken).ConfigureAwait(false);
				if (stream == null)
				{
					throw new InvalidOperationException($"Unable to load image stream from URI '{imageSource.Uri}'.");
				}

				imageData = NSData.FromStream(stream);
				if (imageData == null)
				{
					throw new InvalidOperationException("Unable to load image stream data.");
				}

				if (imageSource.CachingEnabled)
				{
					CacheImage(imageData, pathToImageCache);
				}
			}

			UIImage? image = UIImage.LoadFromData(imageData, scale);
			if (image == null)
			{
				throw new InvalidOperationException($"Unable to decode image from URI '{imageSource.Uri}'.");
			}

			return new ImageSourceServiceResult(image, () => image.Dispose());
		}
		catch (Exception ex)
		{
			Logger?.LogWarning(ex, "Unable to load image URI '{Uri}'.", imageSource.Uri);
			throw;
		}
	}

	/// <summary>Cache image.</summary>
	/// <param name="imageData">Image data.</param>
	/// <param name="path">Image cache path.</param>
	/// <exception cref="InvalidOperationException">Unable to get directory path name.</exception>
	/// <exception cref="InvalidOperationException">Unable to cache image at path.</exception>
	public static void CacheImage(NSData imageData, string path)
	{
		string? directory = Path.GetDirectoryName(path);
		if (string.IsNullOrEmpty(directory))
		{
			throw new InvalidOperationException($"Unable to get directory path name '{path}'.");
		}

		_ = Directory.CreateDirectory(directory);

#pragma warning disable CA1416 // https://github.com/xamarin/xamarin-macios/issues/14619
		bool result = imageData.Save(path, true);
#pragma warning restore CA1416

		if (!result)
		{
			throw new InvalidOperationException($"Unable to cache image at '{path}'.");
		}
	}

	/// <summary>Is image cached.</summary>
	/// <param name="path">Image cache path.</param>
	/// <returns>True if image is found at path.</returns>
	public static bool IsImageCached(string path)
	{
		return File.Exists(path);
	}

	/// <summary>Get cached image.</summary>
	/// <param name="path">Image cache path</param>
	/// <returns>Image Data.</returns>
	/// <exception cref="InvalidOperationException"></exception>
	public static NSData GetCachedImage(string path)
	{
		NSData imageData = NSData.FromFile(path);
		return imageData ?? throw new InvalidOperationException($"Unable to load image stream data from '{path}'.");
	}
}
