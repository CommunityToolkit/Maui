using AView = Android.Views.View;

namespace CommunityToolkit.Maui.Behaviors;

public partial class ImageTouchBehavior
{
	/// <inheritdoc/>
	protected override void OnAttachedTo(VisualElement bindable, AView platformView)
	{
		if (bindable is not Image and not ImageButton)
		{
			throw new InvalidOperationException($"{nameof(ImageTouchBehavior)} can only be attached to an {nameof(Image)} or {nameof(ImageButton)}");
		}

		base.OnAttachedTo(bindable, platformView);
	}
}