using Android.Content;
using Android.Graphics.Drawables;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.Views;

public partial class GravatarImageSourceService
{
	public override async Task<IImageSourceServiceResult?> LoadDrawableAsync(IImageSource imageSource, Android.Widget.ImageView imageView, CancellationToken cancellationToken = default)
	{
		IGravatarImageSource gravatarImageSource = (IGravatarImageSource)imageSource;
		if (!gravatarImageSource.IsEmpty && gravatarImageSource.Uri is not null)
		{
			try
			{
				ImageLoaderCallback callback = new();
				PlatformInterop.LoadImageFromUri(imageView, gravatarImageSource.Uri.OriginalString, new Java.Lang.Boolean(gravatarImageSource.CachingEnabled), callback);
				return await callback.Result;
			}
			catch (Exception ex)
			{
				Logger?.LogWarning(ex, "Unable to load image URI '{Uri}'.", gravatarImageSource.Uri.OriginalString);
				throw;
			}
		}

		return await Task.FromResult<IImageSourceServiceResult?>(null);
	}

	public override async Task<IImageSourceServiceResult<Drawable>?> GetDrawableAsync(IImageSource imageSource, Context context, CancellationToken cancellationToken = default)
	{
		IGravatarImageSource gravatarImageSource = (IGravatarImageSource)imageSource;
		if (!gravatarImageSource.IsEmpty && gravatarImageSource.Uri is not null)
		{
			try
			{
				ImageLoaderResultCallback drawableCallback = new();
				PlatformInterop.LoadImageFromUri(context, gravatarImageSource.Uri.OriginalString, new Java.Lang.Boolean(gravatarImageSource.CachingEnabled), drawableCallback);
				return await drawableCallback.Result;
			}
			catch (Exception ex)
			{
				Logger?.LogWarning(ex, "Unable to load image URI '{Uri}'.", gravatarImageSource.Uri.OriginalString);
				throw;
			}
		}

		return await Task.FromResult<IImageSourceServiceResult<Drawable>?>(null);
	}
}