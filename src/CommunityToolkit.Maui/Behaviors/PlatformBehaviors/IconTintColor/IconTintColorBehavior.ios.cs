using System.ComponentModel;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Behaviors;
public partial class IconTintColorBehavior
{
	/// <inheritdoc/>
	protected override void OnAttachedTo(View bindable, UIView platformView) =>
		ApplyTintColor(platformView, bindable, TintColor);

	/// <inheritdoc/>
	protected override void OnDetachedFrom(View bindable, UIView platformView) =>
		ClearTintColor(platformView, bindable);

	void ClearTintColor(UIView platformView, View element)
	{
		switch (platformView)
		{
			case UIImageView imageView:
				element.PropertyChanged -= OnImageViewTintColorPropertyChanged;
				if (imageView.Image is not null)
				{
					imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
				}

				break;
			case UIButton button:
				element.PropertyChanged -= OnButtonTintColorPropertyChanged;
				if (button.ImageView?.Image is not null)
				{
					var originalImage = button.CurrentImage.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
					button.SetImage(originalImage, UIControlState.Normal);
				}

				break;

			default:
				throw new NotSupportedException($"{nameof(IconTintColorBehavior)} only currently supports {nameof(UIButton)} and {nameof(UIImageView)}.");
		}
	}

	void ApplyTintColor(UIView platformView, View element, Color? color)
	{
		if (color is null)
		{
			ClearTintColor(platformView, element);
			return;
		}

		switch (platformView)
		{
			case UIImageView imageView:
				SetUIImageViewTintColor(imageView, element, color);
				break;
			case UIButton button:
				SetUIButtonTintColor(button, element, color);
				break;
			default:
				throw new NotSupportedException($"{nameof(IconTintColorBehavior)} only currently supports {nameof(UIButton)} and {nameof(UIImageView)}.");
		}
	}

	void SetUIButtonTintColor(UIButton button, View element, Color color)
	{
		if (button.ImageView.Image is null)
		{
			element.PropertyChanged += OnButtonTintColorPropertyChanged;
		}
		else
		{
			var templatedImage = button.CurrentImage.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);

			button.SetImage(null, UIControlState.Normal);
			var platformColor = color.ToPlatform();
			button.TintColor = platformColor;
			button.ImageView.TintColor = platformColor;
			button.SetImage(templatedImage, UIControlState.Normal);
		}
	}

	void OnButtonTintColorPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != ImageButton.IsLoadingProperty.PropertyName
			|| sender is not ImageButton element
			|| element.Handler?.PlatformView is not UIView platformView)
		{
			return;
		}

		if (!element.IsLoading)
		{
			ApplyTintColor(platformView, element, TintColor);
		}
	}

	void SetUIImageViewTintColor(UIImageView imageView, View element, Color color)
	{
		if (imageView.Image == null)
		{
			element.PropertyChanged += OnImageViewTintColorPropertyChanged;
		}
		else
		{
			imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
			imageView.TintColor = color.ToPlatform();
		}
	}

	void OnImageViewTintColorPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != Image.IsLoadingProperty.PropertyName
			|| sender is not Image element
			|| element.Handler?.PlatformView is not UIView platformView)
		{
			return;
		}

		if (!element.IsLoading)
		{
			ApplyTintColor(platformView, element, TintColor);
		}
	}

}