using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tizen.NUI.BaseComponents;
using NView = Tizen.NUI.BaseComponents.View;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A behavior that selects all text when the view is focused.
/// </summary>
public class SelectAllTextBehavior : PlatformBehavior<InputView, NView>
{
	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> or <paramref name="platformView"/> is <see langword="null"/>.</exception>
	protected override void OnAttachedTo(InputView bindable, NView platformView) 
	{ 
		ArgumentNullException.ThrowIfNull(bindable);
		ArgumentNullException.ThrowIfNull(platformView);

		base.OnAttachedTo(bindable, platformView);

		ApplyEffect(true, platformView);
	}

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> or <paramref name="platformView"/> is <see langword="null"/>.</exception>
	protected override void OnDetachedFrom(InputView bindable, NView platformView) 
	{
		ArgumentNullException.ThrowIfNull(bindable);
		ArgumentNullException.ThrowIfNull(platformView);

		base.OnDetachedFrom(bindable, platformView);
		
		ApplyEffect(false, platformView);
	}


	void ApplyEffect(bool apply, NView inputView)
	{
		ArgumentNullException.ThrowIfNull(inputView);

		inputView.FocusGained -= OnFocused;

		if (apply)
		{
			inputView.FocusGained += OnFocused;
		}

		static void OnFocused(object? sender, EventArgs e)
		{
			if (sender is TextField tf && tf.HasFocus())
			{
				tf.SelectWholeText();
			}
			else if (sender is TextEditor te && te.HasFocus())
			{
				te.SelectWholeText();
			}
		}
	}
}