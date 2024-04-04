using AView = Android.Views.View;
using IImage = Microsoft.Maui.IImage;

namespace CommunityToolkit.Maui.Behaviors;

public partial class ImageTouchBehavior
{
	/// <inheritdoc/>
	protected override void OnAttachedTo(VisualElement bindable, AView platformView)
	{
		if (bindable is not IImage)
		{
			throw new InvalidOperationException($"{nameof(ImageTouchBehavior)} can only be attached to an {nameof(IImage)}");
		}

		base.OnAttachedTo(bindable, platformView);
	}
}