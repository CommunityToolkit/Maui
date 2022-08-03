using Android.Content;
using Android.Graphics.Drawables;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

public partial class GravatarImageSourceService
{
	public override async Task<IImageSourceServiceResult?> LoadDrawableAsync(IImageSource imageSource, Android.Widget.ImageView imageView, CancellationToken cancellationToken = default) => await base.LoadDrawableAsync(imageSource, imageView, cancellationToken);

	public override async Task<IImageSourceServiceResult<Drawable>?> GetDrawableAsync(IImageSource imageSource, Context context, CancellationToken cancellationToken = default)
	{
		IGravatarImageSource gravatarImageSource = (IGravatarImageSource)imageSource;
		UriImageSource uriImageSource = new()
		{
			Uri = gravatarImageSource.Uri,
			CacheValidity = gravatarImageSource.CacheValidity,
			CachingEnabled = gravatarImageSource.CachingEnabled,
		};

		return await uriImageSourceService.GetDrawableAsync(uriImageSource, context, cancellationToken);
	}
}