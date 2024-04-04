using System.ComponentModel;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Behaviors;

public partial class IconTintColorBehavior
{
	/// <inheritdoc/>
	protected override void OnViewPropertyChanged(View sender, PropertyChangedEventArgs e)
	{
		base.OnViewPropertyChanged(sender, e);

		if ((e.PropertyName != ImageButton.IsLoadingProperty.PropertyName
			&& e.PropertyName != Image.SourceProperty.PropertyName
			&& e.PropertyName != ImageButton.SourceProperty.PropertyName)
			|| sender is not IImageElement element
			|| sender.Handler?.PlatformView is not UIView platformView)
		{
			return;
		}

		if (!element.IsLoading)
		{
			ApplyTintColor(platformView, (View)element, TintColor);
		}
	}

	/// <inheritdoc/>
	protected override void OnAttachedTo(View bindable, UIView platformView)
	{
		base.OnAttachedTo(bindable, platformView);

		ApplyTintColor(platformView, bindable, TintColor);

		this.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == TintColorProperty.PropertyName)
			{
				ApplyTintColor(platformView, bindable, TintColor);
			}
		};
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(View bindable, UIView platformView)
	{
		base.OnDetachedFrom(bindable, platformView);

		ClearTintColor(platformView, bindable);
	}

	static void ClearTintColor(UIView platformView, View element)
	{
		switch (platformView)
		{
			case UIImageView imageView:
				if (imageView.Image is not null)
				{
					imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
				}

				break;
			case UIButton button:
				if (button.ImageView.Image is not null)
				{
					var originalImage = button.CurrentImage.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
					button.SetImage(originalImage, UIControlState.Normal);
				}

				break;

			default:
				throw new NotSupportedException($"{nameof(IconTintColorBehavior)} only currently supports {nameof(UIButton)} and {nameof(UIImageView)}.");
		}
	}

	static void ApplyTintColor(UIView platformView, View element, Color? color)
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

	static void SetUIButtonTintColor(UIButton button, View element, Color color)
	{
		if (button.ImageView.Image is null)
		{
			return;
		}

		var templatedImage = button.CurrentImage.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);

		button.SetImage(null, UIControlState.Normal);
		var platformColor = color.ToPlatform();
		button.TintColor = platformColor;
		button.ImageView.TintColor = platformColor;
		button.SetImage(templatedImage, UIControlState.Normal);
	}

	static void SetUIImageViewTintColor(UIImageView imageView, View element, Color color)
	{
		if (imageView.Image is null)
		{
			return;
		}

		imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
		imageView.TintColor = color.ToPlatform();
	}
}