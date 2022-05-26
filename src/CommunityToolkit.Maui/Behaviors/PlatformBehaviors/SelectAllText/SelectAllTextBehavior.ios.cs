using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using ObjCRuntime;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A behavior that selects all text when the view is focused.
/// </summary>
public class SelectAllTextBehavior : PlatformBehavior<InputView>
{
	InputView? element;
	UIView? control;

	/// <inheritdoc/>
	protected override void OnAttachedTo(InputView bindable, UIView platformView)
	{
		element = bindable;
		control = platformView;
		ApplyBehaviorToControl(true, platformView);
	}
	
	bool ApplyBehaviorToControl<T>(bool apply, T platformView)
	{
		return platformView switch
		{
			UITextField textField => ApplyToUITextField(textField, apply),
			UITextView => ApplyToUITextView(apply),
			_ => throw new NotSupportedException($"Control of type: {platformView?.GetType()?.Name} is not supported by this effect.")
		};
	}

	bool ApplyToUITextField(UITextField textField, bool shouldApply)
	{
		if (textField is null)
		{
			return false;
		}

		if (shouldApply)
		{
			textField.EditingDidBegin += OnEditingDidBegin;
		}
		else
		{
			textField.EditingDidBegin -= OnEditingDidBegin;
		}

		return true;
	}

	void OnEditingDidBegin(object? sender, EventArgs e)
	{
		if (sender is not UITextField textfield)
		{
			return;
		}

		textfield.PerformSelector(new Selector("selectAll"), null, 0.0f);
	}

	bool ApplyToUITextView(bool shouldApply)
	{
		if (element is not Editor formsControl)
		{
			return false;
		}

		if (shouldApply)
		{
			formsControl.Focused += OnTextViewFocussed;
		}
		else
		{
			formsControl.Focused -= OnTextViewFocussed;
		}

		return true;
	}

	void OnTextViewFocussed(object? sender, FocusEventArgs e)
	{
		if (element is not Editor formsControl || control is not UITextView textView)
		{
			return;
		}

		if (formsControl.IsFocused)
		{
			textView.SelectAll(textView);
		}
	}
}