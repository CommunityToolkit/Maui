using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.Behaviors;

public partial class ImageTouchBehavior
{
	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> or <paramref name="platformView"/> is <see langword="null"/>.</exception>
	protected override void OnAttachedTo(VisualElement bindable, FrameworkElement platformView)
	{
		ArgumentNullException.ThrowIfNull(bindable);
		ArgumentNullException.ThrowIfNull(platformView);

		if (bindable is not Image and not ImageButton)
		{
			throw new InvalidOperationException($"{nameof(ImageTouchBehavior)} can only be attached to an {nameof(Image)} or {nameof(ImageButton)}");
		}

		base.OnAttachedTo(bindable, platformView);
	}
}