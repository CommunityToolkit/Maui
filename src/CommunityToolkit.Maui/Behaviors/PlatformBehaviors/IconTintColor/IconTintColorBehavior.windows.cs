using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WImage = Microsoft.UI.Xaml.Controls.Image;

namespace CommunityToolkit.Maui.Behaviors;

public partial class IconTintColorBehavior
{
	SpriteVisual? currentSpriteVisual;
	CompositionColorBrush? currentColorBrush;

	/// <inheritdoc/>
	protected override void OnAttachedTo(View bindable, FrameworkElement platformView)
	{
		ApplyTintColor(platformView, bindable, TintColor);

		bindable.PropertyChanged += OnElementPropertyChanged;
		this.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == TintColorProperty.PropertyName)
			{
				if (currentColorBrush is not null && TintColor is not null)
				{
					currentColorBrush.Color = TintColor.ToWindowsColor();
				}
				else
				{
					ApplyTintColor(platformView, bindable, TintColor);
				}
			}
		};
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(View bindable, FrameworkElement platformView)
	{
		bindable.PropertyChanged -= OnElementPropertyChanged;
		RemoveTintColor(platformView);
	}

	static bool TryGetButtonImage(WButton button, [NotNullWhen(true)] out WImage? image)
	{
		image = button.Content as WImage;
		return image is not null;
	}

	static bool TryGetSourceImageUri(WImage? imageControl, IImageElement? imageElement, [NotNullWhen(true)] out Uri? uri)
	{
		if (imageElement?.Source is UriImageSource uriImageSource)
		{
			uri = uriImageSource.Uri;
			return true;
		}

		if (imageControl?.Source is BitmapImage bitmapImage)
		{
			uri = bitmapImage.UriSource;
			return true;
		}

		uri = null;
		return false;
	}

	void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not string propertyName
			|| sender is not View bindable
			|| bindable.Handler?.PlatformView is not FrameworkElement platformView)
		{
			return;
		}

		if (propertyName.Equals(Image.SourceProperty.PropertyName, StringComparison.Ordinal)
			|| propertyName.Equals(ImageButton.SourceProperty.PropertyName, StringComparison.Ordinal))
		{
			ApplyTintColor(platformView, bindable, TintColor);
		}
	}

	void ApplyTintColor(FrameworkElement platformView, View element, Color? color)
	{
		RemoveTintColor(platformView);

		if (color is null)
		{
			return;
		}

		switch (platformView)
		{
			case WImage wImage:
				LoadAndApplyImageTintColor(element, wImage, color);
				break;

			case WButton button:
				if (!TryGetButtonImage(button, out var image))
				{
					return;
				}

				LoadAndApplyImageTintColor(element, image, color);
				break;

			default:
				throw new NotSupportedException($"{nameof(IconTintColorBehavior)} only currently supports {typeof(WImage)} and {typeof(WButton)}.");
		}
	}

	void LoadAndApplyImageTintColor(View element, WImage image, Color color)
	{
		image.ImageOpened += OnImageOpened;

		void OnImageOpened(object sender, RoutedEventArgs e)
		{
			image.ImageOpened -= OnImageOpened;

			if (image.ActualSize != Vector2.Zero)
			{
				ApplyImageTintColor(element, image, color);
			}
			else
			{
				// Sometimes the ActualSize of the image is still not ready, therefore we have to wait for the next SizeChange event. 
				image.SizeChanged += OnImageSizeChanged;

				void OnImageSizeChanged(object sender, SizeChangedEventArgs e)
				{
					image.SizeChanged -= OnImageSizeChanged;
					ApplyImageTintColor(element, image, color);
				}
			}
		}
	}

	void ApplyImageTintColor(View element, WImage image, Color color)
	{
		if (!TryGetSourceImageUri(image, (IImageElement)element, out var uri))
		{
			return;
		}

		var width = (float)image.ActualWidth;
		var height = (float)image.ActualHeight;

		// Hide possible visible pixels from original image.
		// Workaround since the tinted image is added as a child to the current image and it's not possible to hide a parent without hiding its children using Visibility.Collapsed.
		image.Width = image.Height = 0;

		// Workaround requires offset to re-center tinted image.
		var offset = new Vector3(-width * .5f, -height * .5f, 0f);

		ApplyTintCompositionEffect(image, color, width, height, offset, uri);
	}

	void ApplyTintCompositionEffect(FrameworkElement platformView, Color color, float width, float height, Vector3 offset, Uri surfaceMaskUri)
	{
		var compositor = ElementCompositionPreview.GetElementVisual(platformView).Compositor;

		currentColorBrush = compositor.CreateColorBrush();
		currentColorBrush.Color = color.ToWindowsColor();

		var loadedSurfaceMask = LoadedImageSurface.StartLoadFromUri(surfaceMaskUri);

		var maskBrush = compositor.CreateMaskBrush();
		maskBrush.Source = currentColorBrush;
		maskBrush.Mask = compositor.CreateSurfaceBrush(loadedSurfaceMask);

		currentSpriteVisual = compositor.CreateSpriteVisual();
		currentSpriteVisual.Brush = maskBrush;
		currentSpriteVisual.Size = new Vector2(width, height);
		currentSpriteVisual.Offset = offset;

		ElementCompositionPreview.SetElementChildVisual(platformView, currentSpriteVisual);
	}

	void RemoveTintColor(FrameworkElement platformView)
	{
		if (currentSpriteVisual is null)
		{
			return;
		}

		switch (platformView)
		{
			case WImage wImage:
				RestoreOriginalImageSize(wImage);
				ElementCompositionPreview.SetElementChildVisual(platformView, null);
				break;

			case WButton button:
				if (TryGetButtonImage(button, out var image))
				{
					RestoreOriginalImageSize(image);
					ElementCompositionPreview.SetElementChildVisual(image, null);
				}
				break;
		}

		currentSpriteVisual.Brush = null;
		currentSpriteVisual = null;
		currentColorBrush = null;
	}

	void RestoreOriginalImageSize(WImage image)
	{
		if (currentSpriteVisual is null)
		{
			return;
		}

		// Restore in Width/Height since ActualSize is readonly
		image.Width = currentSpriteVisual.Size.X;
		image.Height = currentSpriteVisual.Size.Y;
	}
}