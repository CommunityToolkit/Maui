using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A behavior that selects all text when the view is focused.
/// </summary>
public partial class SelectAllTextBehavior : PlatformBehavior<InputView, TextBox>
{
	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> or <paramref name="platformView"/> is <see langword="null"/>.</exception>
	protected override void OnAttachedTo(InputView bindable, TextBox platformView)
	{
		ArgumentNullException.ThrowIfNull(bindable);
		ArgumentNullException.ThrowIfNull(platformView);

		base.OnAttachedTo(bindable, platformView);

		ApplyEffect(true, platformView);
	}

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> or <paramref name="platformView"/> is <see langword="null"/>.</exception>
	protected override void OnDetachedFrom(InputView bindable, TextBox platformView)
	{
		ArgumentNullException.ThrowIfNull(bindable);
		ArgumentNullException.ThrowIfNull(platformView);

		base.OnDetachedFrom(bindable, platformView);

		ApplyEffect(false, platformView);
	}

	void ApplyEffect(bool apply, TextBox editText)
	{
		if (editText is null)
		{
			throw new InvalidOperationException("The Platform View can't be null.");
		}

		editText.GotFocus -= OnGotFocus;

		if (apply)
		{
			editText.GotFocus += OnGotFocus;
		}

		// Static, so that the handler removed when the behavior detaches is the one added when it attached.
		static void OnGotFocus(object sender, RoutedEventArgs e) => (sender as TextBox)?.SelectAll();
	}
}