using CommunityToolkit.Maui.Core;
using UIKit;

namespace CommunityToolkit.Maui.Views;

public partial class GravatarImageSourceService
{
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
	async Task<IImageSourceServiceResult<UIImage>?> GetImageAsync(IGravatarImageSource imageSource, float scale, CancellationToken cancellationToken = default)
	{
		UriImageSource uriImageSource = new()
		{
			Uri = imageSource.Uri,
			CacheValidity = imageSource.CacheValidity,
			CachingEnabled = imageSource.CachingEnabled,
		};

		return await uriImageSourceService.GetImageAsync(uriImageSource, scale, cancellationToken);
	}
}