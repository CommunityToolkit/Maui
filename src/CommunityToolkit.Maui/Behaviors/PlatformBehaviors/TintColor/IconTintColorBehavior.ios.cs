using System.ComponentModel;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Behaviors;
public partial class IconTintColorBehavior : PlatformBehavior<View, UIImageView>
{
	/// <inheritdoc/>
	protected override void OnAttachedTo(View bindable, UIImageView platformView) =>
		ApplyTintColor(platformView, TintColor);

	/// <inheritdoc/>
	protected override void OnDetachedFrom(View bindable, UIImageView platformView) =>
		ClearTintColor(platformView);

	static void ClearTintColor(UIImageView imageView) =>
		imageView.Image = imageView.Image?.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);

	static void ApplyTintColor(UIImageView imageView, Color? color)
	{
		if (color is null)
		{
			ClearTintColor(imageView);
			return;
		}

		imageView.Image = imageView.Image?.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
		imageView.TintColor = color.ToPlatform();
	}

	void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs args)
	{
		if (args.PropertyName is not string propertyName
			|| sender is not View bindable
			|| bindable.Handler?.PlatformView is not UIImageView platformView)
		{
			return;
		}

		if (!propertyName.Equals(TintColorProperty.PropertyName)
			&& !propertyName.Equals(Image.SourceProperty.PropertyName)
			&& !propertyName.Equals(ImageButton.SourceProperty.PropertyName))
		{
			return;
		}

		ApplyTintColor(platformView, TintColor);
	}
}
