namespace CommunityToolkit.Maui.Views;

using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui;
using WImageSource = Microsoft.UI.Xaml.Media.ImageSource;

public partial class GravatarImageSourceService
{
	/// <summary>Get image source.</summary>
	/// <param name="imageSource">Image source</param>
	/// <param name="scale">Scale.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>UriImageSourceService result.</returns>
	public override Task<IImageSourceServiceResult<WImageSource>?> GetImageSourceAsync(IImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default) => GetImageSourceAsync((IGravatarImageSource)imageSource, cancellationToken);

	/// <summary>Get image source.</summary>
	/// <param name="imageSource">Image source</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>UriImageSourceService result.</returns>
	async Task<IImageSourceServiceResult<WImageSource>?> GetImageSourceAsync(IGravatarImageSource imageSource, CancellationToken cancellationToken = default)
	{
		UriImageSource uriImageSource = new()
		{
			Uri = imageSource.Uri,
			CacheValidity = imageSource.CacheValidity,
			CachingEnabled = imageSource.CachingEnabled,
		};

		return await uriImageSourceService.GetImageSourceAsync(uriImageSource, 1, cancellationToken);
	}
}