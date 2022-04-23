using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using ObjCRuntime;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// 
/// </summary>
public class SelectAllText : PlatformBehavior<InputView>
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

	bool ApplyToUITextField(UITextField textField, bool apply)
	{
		if (textField is null)
		{
			return false;
		}

		if (apply)
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

	bool ApplyToUITextView(bool apply)
	{
		if (element is not Editor formsControl)
		{
			return false;
		}

		if (apply)
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