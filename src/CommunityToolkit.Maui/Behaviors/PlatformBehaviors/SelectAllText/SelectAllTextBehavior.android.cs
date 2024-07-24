using Android.Widget;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A behavior that selects all text when the view is focused.
/// </summary>
public class SelectAllTextBehavior : BasePlatformBehavior<InputView, EditText>
{
	/// <inheritdoc/>
	protected override void OnAttachedTo(InputView bindable, EditText platformView)
	{
		base.OnAttachedTo(bindable, platformView);

		platformView.SetSelectAllOnFocus(true);
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(InputView bindable, EditText platformView)
	{
		base.OnDetachedFrom(bindable, platformView);

		platformView.SetSelectAllOnFocus(false);
	}
}