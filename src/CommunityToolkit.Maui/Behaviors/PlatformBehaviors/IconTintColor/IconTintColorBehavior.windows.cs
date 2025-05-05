using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Microsoft.Maui.Platform;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WImageSource = Microsoft.UI.Xaml.Media.ImageSource;

namespace CommunityToolkit.Maui.Behaviors;

public partial class IconTintColorBehavior
{
	SpriteVisual? currentSpriteVisual;
	CompositionColorBrush? currentColorBrush;
	BitmapSource? blankImage;
	WImageSource? originalImage;

	/// <inheritdoc/>
	protected override void OnAttachedTo(View bindable, FrameworkElement platformView)
	{
		base.OnAttachedTo(bindable, platformView);

		ApplyTintColor(platformView, bindable, TintColor);

		PropertyChanged += OnIconTintColorBehaviorPropertyChanged;
		bindable.PropertyChanged += OnElementPropertyChanged;
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(View bindable, FrameworkElement platformView)
	{
		base.OnDetachedFrom(bindable, platformView);

		bindable.PropertyChanged -= OnElementPropertyChanged;
		PropertyChanged -= OnIconTintColorBehaviorPropertyChanged;

		RemoveTintColor(platformView);
	}

	static bool TryGetButtonImage(WButton button, [NotNullWhen(true)] out WImage? image)
	{
		image = button.Content switch
		{
			WImage windowsImage => windowsImage,
			Microsoft.UI.Xaml.Controls.Panel panel => panel.Children?.OfType<WImage>().FirstOrDefault(),
			_ => null
		};

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

	void OnIconTintColorBehaviorPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);
		var iconTintColorBehavior = (IconTintColorBehavior)sender;

		if (iconTintColorBehavior.View is not IView bindable
			|| bindable.Handler?.PlatformView is not FrameworkElement platformView)
		{
			return;
		}

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
	}

	void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not string propertyName
			|| sender is not IView bindable
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

	void ApplyTintColor(FrameworkElement platformView, IView element, Color? color)
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
				throw new NotSupportedException($"{nameof(IconTintColorBehavior)} only currently supports {typeof(WImage).FullName} and {typeof(WButton).FullName}.");
		}
	}

	void LoadAndApplyImageTintColor(IView element, WImage image, Color color)
	{
		if (element is IImageElement { Source: UriImageSource uriImageSource })
		{
			image.Source = Path.GetExtension(uriImageSource.Uri.AbsolutePath) switch
			{
				var extension when extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) => new SvgImageSource(uriImageSource.Uri),
				_ => new BitmapImage(uriImageSource.Uri)
			};

			ApplyTintColor();
		}
		else if (image.IsLoaded)
		{
			// Sometimes WImage source doesn't match View source so the image is not ready to be tinted
			// We must wait for next ImageOpened event
			if (element is IImageElement { Source: FileImageSource fileImageSource }
				&& image.Source is BitmapImage bitmapImage
				&& Uri.Compare(new Uri($"{bitmapImage.UriSource.Scheme}:///{fileImageSource.File}"), bitmapImage.UriSource, UriComponents.Path, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) is not 0)
			{
				image.ImageOpened += OnImageOpened;
			}
			else
			{
				ApplyTintColor();
			}
		}
		else
		{
			image.ImageOpened += OnImageOpened;
		}

		void OnImageOpened(object sender, RoutedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);

			var image = (WImage)sender;
			image.ImageOpened -= OnImageOpened;

			ApplyTintColor();
		}

		void ApplyTintColor()
		{
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
					ArgumentNullException.ThrowIfNull(sender);
					var image = (WImage)sender;

					image.SizeChanged -= OnImageSizeChanged;
					ApplyImageTintColor(element, image, color);
				}
			}
		}
	}

	void ApplyImageTintColor(IView element, WImage image, Color color)
	{
		if (!TryGetSourceImageUri(image, (IImageElement)element, out var uri))
		{
			return;
		}

		var width = (float)image.ActualWidth;
		var height = (float)image.ActualHeight;
		var anchorPoint = new Vector2((float)element.AnchorX, (float)element.AnchorY);

		// Requested size requires additional offset to re-center tinted image.
		var offset = new Vector3(width * anchorPoint.X, height * anchorPoint.Y, 0f);

		ApplyTintCompositionEffect(image, color, width, height, offset, anchorPoint, uri);

		// Hide possible visible pixels from original image by replacing with a transparent image of the same size
		if (blankImage is null
			|| (blankImage.PixelWidth != (int)width && blankImage.PixelHeight != (int)height))
		{
			// Source image has changed, update the cached blank image
			blankImage = new WriteableBitmap((int)width, (int)height);
		}

		originalImage = image.Source;
		image.Source = blankImage;
	}

	void ApplyTintCompositionEffect(FrameworkElement platformView, Color color, float width, float height, Vector3 offset, Vector2 anchorPoint, Uri surfaceMaskUri)
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
		currentSpriteVisual.AnchorPoint = anchorPoint;

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
				RestoreOriginalImage(wImage);
				ElementCompositionPreview.SetElementChildVisual(platformView, null);
				break;

			case WButton button:
				if (TryGetButtonImage(button, out var image))
				{
					RestoreOriginalImage(image);
					ElementCompositionPreview.SetElementChildVisual(image, null);
				}
				break;

			default:
				throw new NotSupportedException($"{nameof(IconTintColorBehavior)} only currently supports {typeof(WImage).FullName} and {typeof(WButton).FullName}.");
		}

		currentSpriteVisual.Brush = null;
		currentSpriteVisual = null;
		currentColorBrush = null;
	}

	void RestoreOriginalImage(WImage image)
	{
		if (currentSpriteVisual is null)
		{
			return;
		}

		image.Source = originalImage;
	}
}