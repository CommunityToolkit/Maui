using System.ComponentModel;
using System.Numerics;
using Microsoft.Maui.Platform;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Visibility = Microsoft.UI.Xaml.Visibility;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WButton = Microsoft.UI.Xaml.Controls.Button;

namespace CommunityToolkit.Maui.Behaviors;

public partial class IconTintColorBehavior
{
	SpriteVisual? spriteVisual;
	Vector2? originalImageSize;

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
	}

	void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != ImageButton.IsLoadingProperty.PropertyName
		    || e.PropertyName != Image.SourceProperty.PropertyName
		    || e.PropertyName != ImageButton.SourceProperty.PropertyName
		    || sender is not IImageElement element
		    || (sender as VisualElement)?.Handler?.PlatformView is not FrameworkElement platformView)
		{
			return;
		}

		if (!element.IsLoading)
		{
			ApplyTintColor(platformView, (View)element, TintColor);
		}
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
				if (image.ActualSize != Vector2.Zero)
				{
					ApplyImageTintColor(element, image, color);
				}
				else
				{
					void OnImageSizeInitialized(object sender, SizeChangedEventArgs e)
					{
						image.SizeChanged -= OnImageSizeInitialized;
						ApplyImageTintColor(element, image, color);
					}

					image.SizeChanged += OnImageSizeInitialized;
				}
				break;
			}
			case WButton button:
			{
				var image = TryGetButtonImage(button);
				if (image is null)
				{
					return;
				}

				if (image.ActualSize != Vector2.Zero)
				{
					ApplyButtonImageTintColor(element, button, image, color);
				}
				else
				{
					void OnButtonImageSizeInitialized(object sender, SizeChangedEventArgs e)
					{
						image.SizeChanged -= OnButtonImageSizeInitialized;	
						ApplyButtonImageTintColor(element, button, image, color);
					}

					image.SizeChanged += OnButtonImageSizeInitialized;
				}
				break;
			}
			default:
				throw new NotSupportedException(
					$"{nameof(IconTintColorBehavior)} only currently supports {nameof(WImage)} and {nameof(WButton)}.");
		}
	}

	void ApplyButtonImageTintColor(View element, WButton button, WImage image, Color color)
	{
		var offset = image.ActualOffset;
		var width = (float)image.ActualWidth;
		var height = (float)image.ActualHeight;

		var uri = TryGetSourceImageUri(image, element as IImageElement);
		if (uri is null)
		{
			return;
		}

		// Hide possible visible pixels from original image
		image.Visibility = Visibility.Collapsed;

		ApplyTintCompositionEffect(button, color, width, height, offset, uri);
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
		// Workaround as it's not possible to hide parent without also hiding children using Visibility.Collapsed.
		image.Width = image.Height = 0;

		// Workaround requires offset to re-center tinted image
		var offset = new Vector3(-width * .5f, -height * .5f, 0f);

		ApplyTintCompositionEffect(image, color, width, height, offset, uri);
	}

	Vector2 GetTintImageSize(WImage image)
	{
		// ActualSize is set by the renderer when loaded. Without the zero size workaround, it's usually always what we want (default). 
		if (image.ActualSize != Vector2.Zero)
		{
			return image.ActualSize;
		}

		// (Fallback 1) Required when the Source property changes, because the size has been set to zero to hide the original image.
		if (originalImageSize.HasValue)
		{
			return originalImageSize.Value;
		}

		// (Fallback 2) Required when previous effect was removed and image was hidden using zero size workaround.
		// The image size is restored in Width/Height during OnDetach.
		// However the values are not reflected in the "ActualSize", therefore this extra fallback is required.
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
		spriteVisual.AnchorPoint = Vector2.Zero;
		spriteVisual.CenterPoint = new Vector3(width * .5f, height * .5f, 0f);
		spriteVisual.Offset = offset;
		spriteVisual.BorderMode = CompositionBorderMode.Soft;

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
					image.Visibility = Visibility.Visible;
				}
				break;
			}
		}

		spriteVisual.Brush = null;
		ElementCompositionPreview.SetElementChildVisual(platformView, null);
	}

	void RestoreOriginalImageSize(WImage image)
	{
		if (!originalImageSize.HasValue)
		{
			return;
		}

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
