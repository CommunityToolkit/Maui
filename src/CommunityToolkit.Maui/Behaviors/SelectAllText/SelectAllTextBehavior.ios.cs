using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using UIKit;

namespace CommunityToolkit.Maui.Behaviors;
public partial class SelectAllTextBehavior
{
	UIView? nativeView;
	partial void OnPlatformkAttachedBehavior(InputView view)
	{
		nativeView = view.ToNative(view.Handler.MauiContext!);
		ApplyEffect(true);
	}
	partial void OnPlatformDeattachedBehavior(InputView view)
	{
		ApplyEffect(false);
	}

	void ApplyEffect(bool apply) => ApplyToControl(nativeView, apply);

	bool ApplyToControl<T>(T controlType, bool apply) => controlType switch
	{
		UITextField textField => ApplyToUITextField(textField, apply),
		UITextView => ApplyToUITextView(apply),
		_ => throw new NotSupportedException($"Control of type: {controlType?.GetType()?.Name} is not supported by this effect.")
	};

	bool ApplyToUITextView(bool apply)
	{
		if (View is not Editor formsControl)
			return false;

		if (apply)
			formsControl.Focused += OnTextViewFocussed;
		else
			formsControl.Focused -= OnTextViewFocussed;

		return true;
	}

	void OnTextViewFocussed(object? sender, FocusEventArgs e)
	{
		if (View is not Editor formsControl || nativeView is not UITextView textView)
			return;

		if (formsControl.IsFocused)
			textView.SelectAll(textView);
	}

	bool ApplyToUITextField(UITextField textField, bool apply)
	{
		if (textField == null)
			return false;

		if (apply)
			textField.EditingDidBegin += OnEditingDidBegin;
		else
			textField.EditingDidBegin -= OnEditingDidBegin;

		return true;
	}

	void OnEditingDidBegin(object? sender, EventArgs e)
	{
		if (sender is not UITextField textfield)
			return;

		textfield.PerformSelector(new ObjCRuntime.Selector("selectAll"), null, 0.0f);
	}
}
