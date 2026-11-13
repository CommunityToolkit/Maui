using Android.Widget;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A behavior that selects all text when the view is focused.
/// </summary>
public class SelectAllTextBehavior : BasePlatformBehavior<InputView, EditText>
{
	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> or <paramref name="platformView"/> is <see langword="null"/>.</exception>
	protected override void OnAttachedTo(InputView bindable, EditText platformView)
	{
		ArgumentNullException.ThrowIfNull(bindable);
		ArgumentNullException.ThrowIfNull(platformView);

		base.OnAttachedTo(bindable, platformView);

		platformView.SetSelectAllOnFocus(true);
	}

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> or <paramref name="platformView"/> is <see langword="null"/>.</exception>
	protected override void OnDetachedFrom(InputView bindable, EditText platformView)
	{
		ArgumentNullException.ThrowIfNull(bindable);
		ArgumentNullException.ThrowIfNull(platformView);

		base.OnDetachedFrom(bindable, platformView);

		platformView.SetSelectAllOnFocus(false);
	}
}