using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Behaviors;
public partial class IconTintColorBehavior : PlatformBehavior<Image, UIImageView>
{
	/// <inheritdoc/>
	protected override void OnAttachedTo(Image bindable, UIImageView platformView) =>
		ApplyTintColor(platformView, TintColor);

	/// <inheritdoc/>
	protected override void OnDetachedFrom(Image bindable, UIImageView platformView) =>
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
}
