using System.ComponentModel;
using System.Numerics;
using Microsoft.Maui.Platform;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WButton = Microsoft.UI.Xaml.Controls.Button;

namespace CommunityToolkit.Maui.Behaviors;

public partial class IconTintColorBehavior
{
	SpriteVisual? spriteVisual;
	Vector2? originalImageSize;
	bool IsUpdate => originalImageSize is not null;

	/// <inheritdoc/>
	protected override void OnAttachedTo(View bindable, FrameworkElement platformView)
	{
		ApplyTintColor(platformView, bindable, TintColor);

		bindable.PropertyChanged += OnElementPropertyChanged;
		this.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == TintColorProperty.PropertyName)
			{
				ApplyTintColor(platformView, bindable, TintColor);
			}
		};
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(View bindable, FrameworkElement platformView)
	{
		bindable.PropertyChanged -= OnElementPropertyChanged;
		RemoveTintColor(platformView);
		originalImageSize = null;
	}

	void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not string propertyName
		    || sender is not View bindable
		    || bindable.Handler?.PlatformView is not FrameworkElement platformView)
		{
			return;
		}

		if (!propertyName.Equals(Image.SourceProperty.PropertyName, StringComparison.Ordinal)
		    && !propertyName.Equals(ImageButton.SourceProperty.PropertyName, StringComparison.Ordinal))
		{
			return;
		}

		ApplyTintColor(platformView, bindable, TintColor);
	}

	void ApplyTintColor(FrameworkElement platformView, View element, Color? color)
	{
		if (color is null)
		{
			RemoveTintColor(platformView);
			return;
		}

		switch (platformView)
		{
			case WImage image:
			{
				LoadAndApplyImageTintColor(element, image, color);
				break;
			}
			case WButton button:
			{
				var image = TryGetButtonImage(button);
				if (image is null)
				{
					return;
				}

				LoadAndApplyImageTintColor(element, image, color);
				break;
			}
			default:
				throw new NotSupportedException(
					$"{nameof(IconTintColorBehavior)} only currently supports {nameof(WImage)} and {nameof(WButton)}.");
		}
	}

	void LoadAndApplyImageTintColor(View element, WImage image, Color color)
	{
		// There seems to be no other indicator if the image is loaded and the ActualSize is available.
		var isLoaded = image.ActualSize != Vector2.Zero;

		if (isLoaded || IsUpdate)
		{
			ApplyImageTintColor(element, image, color);
		}
		else
		{
			void OnButtonImageSizeInitialized(object sender, SizeChangedEventArgs e)
			{
				image.SizeChanged -= OnButtonImageSizeInitialized;
				ApplyImageTintColor(element, image, color);
			}

			image.SizeChanged += OnButtonImageSizeInitialized;
		}
	}


	void ApplyImageTintColor(View element, WImage image, Color color)
	{
		var uri = TryGetSourceImageUri(image, element as IImageElement);
		if (uri is null)
		{
			return;
		}

		originalImageSize = GetTintImageSize(image);
		var width = originalImageSize.Value.X;
		var height = originalImageSize.Value.Y;

		// Hide possible visible pixels from original image.
		// Workaround since the tinted image is added as a child to the current image and it's not possible to hide a parent without hiding its children using Visibility.Collapsed.
		image.Width = image.Height = 0;

		// Workaround requires offset to re-center tinted image.
		var offset = new Vector3(-width * .5f, -height * .5f, 0f);

		ApplyTintCompositionEffect(image, color, width, height, offset, uri);
	}

	Vector2 GetTintImageSize(WImage image)
	{
		// ActualSize is set by the layout process when loaded. Without the zero size workaround, it's always what we want (default). 
		if (image.ActualSize != Vector2.Zero)
		{
			return image.ActualSize;
		}

		if (originalImageSize.HasValue)
		{
			return originalImageSize.Value;
		}

		return new Vector2((float)image.Width, (float)image.Height);
	}

	void ApplyTintCompositionEffect(FrameworkElement platformView, Color color, float width, float height,
		Vector3 offset, Uri surfaceMaskUri)
	{
		var compositor = ElementCompositionPreview.GetElementVisual(platformView).Compositor;

		var sourceColorBrush = compositor.CreateColorBrush();
		sourceColorBrush.Color = color.ToWindowsColor();

		var loadedSurfaceMask = LoadedImageSurface.StartLoadFromUri(surfaceMaskUri);

		var maskBrush = compositor.CreateMaskBrush();
		maskBrush.Source = sourceColorBrush;
		maskBrush.Mask = compositor.CreateSurfaceBrush(loadedSurfaceMask);

		spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Brush = maskBrush;
		spriteVisual.Size = new Vector2(width, height);
		spriteVisual.Offset = offset;

		ElementCompositionPreview.SetElementChildVisual(platformView, spriteVisual);
	}

	void RemoveTintColor(FrameworkElement platformView)
	{
		if (spriteVisual is null)
		{
			return;
		}

		switch (platformView)
		{
			case WImage image:
			{
				RestoreOriginalImageSize(image);
				break;
			}
			case WButton button:
			{
				var image = TryGetButtonImage(button);
				if (image is not null)
				{
					RestoreOriginalImageSize(image);
				}
				break;
			}
		}

		spriteVisual.Brush = null;
		spriteVisual = null;
		ElementCompositionPreview.SetElementChildVisual(platformView, null);
	}

	void RestoreOriginalImageSize(WImage image)
	{
		if (originalImageSize is null)
		{
			return;
		}

		// Restore in Width/Height since ActualSize is readonly
		image.Width = originalImageSize.Value.X;
		image.Height = originalImageSize.Value.Y;
	}

	static Uri? TryGetSourceImageUri(WImage? imageControl, IImageElement? imageElement)
	{
		if (imageElement?.Source is UriImageSource uriImageSource)
		{
			return uriImageSource.Uri;
		}

		if (imageControl?.Source is BitmapImage bitmapImage)
		{
			return bitmapImage.UriSource;
		}

		return null;
	}

	static WImage? TryGetButtonImage(WButton button)
	{
		return button.Content as WImage;
	}
}
