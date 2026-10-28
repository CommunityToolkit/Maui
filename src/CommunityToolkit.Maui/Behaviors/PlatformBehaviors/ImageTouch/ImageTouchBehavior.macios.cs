using UIKit;

namespace CommunityToolkit.Maui.Behaviors;

public partial class ImageTouchBehavior
{
	/// <inheritdoc/>
	protected override void OnAttachedTo(VisualElement bindable, UIView platformView)
	{
		if (bindable is not Image and not ImageButton)
		{
			throw new InvalidOperationException($"{nameof(ImageTouchBehavior)} can only be attached to an {nameof(Image)} or {nameof(ImageButton)}");
		}

		base.OnAttachedTo(bindable, platformView);
	}
}