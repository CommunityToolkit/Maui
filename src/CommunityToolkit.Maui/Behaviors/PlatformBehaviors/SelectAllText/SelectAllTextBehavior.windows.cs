using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A behavior that selects all text when the view is focused.
/// </summary>
public class SelectAllTextBehavior : PlatformBehavior<InputView, TextBox>
{
	/// <inheritdoc/>
	protected override void OnAttachedTo(InputView bindable, TextBox platformView)
	{
		base.OnAttachedTo(bindable, platformView);

		ApplyEffect(true, platformView);
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(InputView bindable, TextBox platformView)
	{
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

		void OnGotFocus(object sender, RoutedEventArgs e) => editText?.SelectAll();
	}
}