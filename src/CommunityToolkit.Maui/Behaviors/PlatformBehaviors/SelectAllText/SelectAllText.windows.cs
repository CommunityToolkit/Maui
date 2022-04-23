using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// 
/// </summary>
public class SelectAllText : PlatformBehavior<InputView, TextBox>
{
	/// <inheritdoc/>
	protected override void OnAttachedTo(InputView bindable, TextBox platformView)
	{
		ApplyEffect(true, platformView);
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(InputView bindable, TextBox platformView)
	{
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
