using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Platform;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MImageSource = Microsoft.Maui.Controls.ImageSource;
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

	// A tint that was removed must not be applied when its image later opens or gets its size.
	Action? cancelPendingTint;

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

	static bool TryGetSourceImageUri(WImage? imageControl, MImageSource? imageSource, [NotNullWhen(true)] out Uri? uri)
	{
		if (imageSource is UriImageSource uriImageSource)
		{
			uri = uriImageSource.Uri;
			return true;
		}

		// A BitmapImage that MAUI loaded from a stream has no UriSource (see TryGetSourceImageStream).
		if (imageControl?.Source is BitmapImage { UriSource: Uri bitmapImageUri })
		{
			uri = bitmapImageUri;
			return true;
		}

		uri = null;
		return false;
	}

	// MAUI loads a StreamImageSource, and a FileImageSource with a rooted path, from a stream into a BitmapImage without a UriSource: the mask
	// of the tint is loaded from the same stream.
	static bool TryGetSourceImageStream(MImageSource? imageSource, [NotNullWhen(true)] out Func<CancellationToken, Task<Stream>>? openStream)
	{
		openStream = imageSource switch
		{
			FileImageSource { File: string file } when Path.IsPathRooted(file) => _ => Task.FromResult<Stream>(File.OpenRead(file)),
			StreamImageSource { Stream: Func<CancellationToken, Task<Stream>> stream } => stream,
			_ => null
		};

		return openStream is not null;
	}

	static bool IsImageFullyMeasured(WImage image) => image.ActualWidth > 0 && image.ActualHeight > 0;

	static MImageSource? GetImageSource(IView element) => element switch
	{
		Image image => image.Source,
		ImageButton imageButton => imageButton.Source,
		Button button => button.ImageSource,
		_ => throw new NotSupportedException($"The type {element.GetType().FullName} is not supported.")
	};

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
			|| propertyName.Equals(ImageButton.SourceProperty.PropertyName, StringComparison.Ordinal)
			|| propertyName.Equals(Button.ImageSourceProperty.PropertyName, StringComparison.Ordinal))
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
		var isSizeChangedSubscribed = false;

		if (GetImageSource(element) is UriImageSource uriImageSource)
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
			// That includes a WImage without a source yet: MAUI raises PropertyChanged(Source) before its handler sets the image
			// A BitmapImage without a UriSource comes from a stream, which cannot be compared with the FileImageSource: it is taken as its image
			if (image.Source is null
				|| (GetImageSource(element) is FileImageSource fileImageSource
					&& image.Source is BitmapImage { UriSource: Uri bitmapImageUri }
					&& Uri.Compare(new Uri($"{bitmapImageUri.Scheme}:///{fileImageSource.File}"), bitmapImageUri, UriComponents.Path, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) is not 0))
			{
				WaitForImageOpened();
			}
			else
			{
				ApplyTintColor();
			}
		}
		else
		{
			WaitForImageOpened();
		}

		void WaitForImageOpened()
		{
			image.ImageOpened += OnImageOpened;
			cancelPendingTint = () => image.ImageOpened -= OnImageOpened;
		}

		void OnImageOpened(object sender, RoutedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);

			var image = (WImage)sender;
			image.ImageOpened -= OnImageOpened;
			cancelPendingTint = null;

			ApplyTintColor();
		}

		void ApplyTintColor()
		{
			if (IsImageFullyMeasured(image))
			{
				ApplyImageTintColor(element, image, color);
				return;
			}

			// Sometimes the ActualSize of the image is still not ready, therefore we have to wait for the next SizeChange event.
			SubscribeToSizeChanged();

			void SubscribeToSizeChanged()
			{
				if (isSizeChangedSubscribed)
				{
					return;
				}

				image.SizeChanged += OnImageSizeChanged;
				isSizeChangedSubscribed = true;
				cancelPendingTint = () => image.SizeChanged -= OnImageSizeChanged;

				void OnImageSizeChanged(object sender, SizeChangedEventArgs e)
				{
					ArgumentNullException.ThrowIfNull(sender);
					var image = (WImage)sender;

					if (!IsImageFullyMeasured(image))
					{
						return;
					}

					image.SizeChanged -= OnImageSizeChanged;
					isSizeChangedSubscribed = false;
					cancelPendingTint = null;
					ApplyImageTintColor(element, image, color);
				}
			}
		}
	}

	void ApplyImageTintColor(IView element, WImage image, Color color)
	{
		var imageSource = GetImageSource(element);
		Func<CancellationToken, Task<Stream>>? openMaskStream = null;

		if (!TryGetSourceImageUri(image, imageSource, out var maskUri) && !TryGetSourceImageStream(imageSource, out openMaskStream))
		{
			return;
		}

		if (!IsImageFullyMeasured(image))
		{
			return;
		}

		var width = (float)image.ActualWidth;
		var height = (float)image.ActualHeight;
		var anchorPoint = new Vector2((float)element.AnchorX, (float)element.AnchorY);

		// Requested size requires additional offset to re-center tinted image.
		var offset = new Vector3(width * anchorPoint.X, height * anchorPoint.Y, 0f);

		var surfaceMask = ApplyTintCompositionEffect(image, color, width, height, offset, anchorPoint, maskUri);

		var pixelWidth = Math.Max(1, (int)Math.Ceiling(image.ActualWidth));
		var pixelHeight = Math.Max(1, (int)Math.Ceiling(image.ActualHeight));

		// Hide possible visible pixels from original image by replacing with a transparent image of the same size
		if (blankImage is null
			|| blankImage.PixelWidth != pixelWidth
			|| blankImage.PixelHeight != pixelHeight)
		{
			// Source image has changed, update the cached blank image
			blankImage = new WriteableBitmap(pixelWidth, pixelHeight);
		}

		originalImage = image.Source;
		image.Source = blankImage;

		// The tint must be in place first: LoadSurfaceMaskAsync removes it when the stream cannot be read.
		if (openMaskStream is not null)
		{
			LoadSurfaceMaskAsync(image, surfaceMask, openMaskStream).SafeFireAndForget(static ex => Trace.WriteLine(ex));
		}
	}

	CompositionSurfaceBrush ApplyTintCompositionEffect(FrameworkElement platformView, Color color, float width, float height, Vector3 offset, Vector2 anchorPoint, Uri? surfaceMaskUri)
	{
		var compositor = ElementCompositionPreview.GetElementVisual(platformView).Compositor;

		currentColorBrush = compositor.CreateColorBrush();
		currentColorBrush.Color = color.ToWindowsColor();

		var surfaceMask = compositor.CreateSurfaceBrush();

		if (surfaceMaskUri is not null)
		{
			surfaceMask.Surface = LoadedImageSurface.StartLoadFromUri(surfaceMaskUri);
		}

		var maskBrush = compositor.CreateMaskBrush();
		maskBrush.Source = currentColorBrush;
		maskBrush.Mask = surfaceMask;

		currentSpriteVisual = compositor.CreateSpriteVisual();
		currentSpriteVisual.Brush = maskBrush;
		currentSpriteVisual.Size = new Vector2(width, height);
		currentSpriteVisual.Offset = offset;
		currentSpriteVisual.AnchorPoint = anchorPoint;

		ElementCompositionPreview.SetElementChildVisual(platformView, currentSpriteVisual);

		return surfaceMask;
	}

	// Called on the UI thread, where it continues (LoadedImageSurface is a XAML object). LoadedImageSurface reads the stream after
	// StartLoadFromStream returned, so it gets a copy in memory, and the stream of the image source is closed.
	async Task LoadSurfaceMaskAsync(WImage image, CompositionSurfaceBrush surfaceMask, Func<CancellationToken, Task<Stream>> openStream)
	{
		var tintVisual = currentSpriteVisual;
		var maskStream = new MemoryStream();

		try
		{
			await using (var stream = await openStream(CancellationToken.None))
			{
				await stream.CopyToAsync(maskStream);
			}
		}
		catch (Exception)
		{
			// The image source cannot give its image a second time, e.g. ImageSource.FromStream(() => stream) gives the stream that MAUI
			// read and disposed: without a mask, the tint would hide the image, so the image is shown untinted.
			if (tintVisual is not null && ReferenceEquals(tintVisual, currentSpriteVisual))
			{
				RemoveTintColor(image);
			}

			throw;
		}

		maskStream.Position = 0;
		surfaceMask.Surface = LoadedImageSurface.StartLoadFromStream(maskStream.AsRandomAccessStream());
	}

	void RemoveTintColor(FrameworkElement platformView)
	{
		cancelPendingTint?.Invoke();
		cancelPendingTint = null;

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