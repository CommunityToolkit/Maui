namespace CommunityToolkit.Maui.Views;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using WImageSource = Microsoft.UI.Xaml.Media.ImageSource;

public partial class GravatarImageSourceService
{
	/// <summary>Get image source.</summary>
	/// <param name="imageSource">Image source</param>
	/// <param name="scale">Scale.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Image source service result.</returns>
	public override Task<IImageSourceServiceResult<WImageSource>?> GetImageSourceAsync(IImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default) => GetImageSourceAsync((IGravatarImageSource)imageSource, cancellationToken);

	/// <summary>Get image source.</summary>
	/// <param name="imageSource">Image source</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Image source service result.</returns>
	/// <exception cref="InvalidOperationException">Unable to load image stream.</exception>
	public async Task<IImageSourceServiceResult<WImageSource>?> GetImageSourceAsync(IGravatarImageSource imageSource, CancellationToken cancellationToken = default)
	{
		if (imageSource.IsEmpty)
		{
			return null;
		}

		// TODO: use a real caching library with the URI
		if (imageSource is not IStreamImageSource streamImageSource)
		{
			throw new InvalidOperationException("Unable to load URI as a stream.");
		}

		try
		{
			using var stream = await streamImageSource.GetStreamAsync(cancellationToken);
			if (stream == null)
			{
				throw new InvalidOperationException("Unable to load image stream.");
			}

			var image = new BitmapImage();
			using var ras = stream.AsRandomAccessStream();
			await image.SetSourceAsync(ras);
			var result = new ImageSourceServiceResult(image);

			return result;
		}
		catch (Exception ex)
		{
			Logger?.LogWarning(ex, "Unable to load image URI '{Uri}'.", imageSource.Uri);
			throw;
		}
	}
}