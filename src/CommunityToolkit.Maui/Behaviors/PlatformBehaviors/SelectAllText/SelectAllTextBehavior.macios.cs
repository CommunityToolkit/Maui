using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using ObjCRuntime;
using System.Runtime.Versioning;

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

	/// <inheritdoc/>
	protected override void OnDetachedFrom(InputView bindable, UIView platformView)
	{
		element = bindable;
		control = platformView;
		ApplyBehaviorToControl(false, platformView);
	}

	bool ApplyBehaviorToControl<T>(bool apply, T platformView) => platformView switch
	{
		UITextField textField => ApplyToUITextField(textField, apply),
		UITextView => ApplyToUITextView(apply),
		_ => throw new NotSupportedException($"Control of type: {platformView?.GetType()?.Name} is not supported by this effect.")
	};

	bool ApplyToUITextField(UITextField textField, bool shouldApply)
	{
		if (textField is null)
		{
			throw new InvalidOperationException("The Platform View can't be null.");
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
			throw new InvalidOperationException($"The Platform View should be of the type {nameof(UITextField)}.");
		}

		textfield.PerformSelector(new Selector("selectAll"), null, 0.0f);
	}

	// MacCatalyst support blocked: https://github.com/xamarin/xamarin-macios/issues/15156
	bool ApplyToUITextView(bool shouldApply)
	{
		if (OperatingSystem.IsMacCatalyst())
		{
			Console.WriteLine("MacCatalyst doesn't support this feature on the editor. for more info see https://github.com/CommunityToolkit/Maui/issues/432");
		}

		if (element is not Editor mauiControl)
		{
			throw new InvalidOperationException($"The Maui control should be of the type {nameof(Editor)}.");
		}

		if (shouldApply)
		{
			mauiControl.Focused += OnTextViewFocussed;
		}
		else
		{
			mauiControl.Focused -= OnTextViewFocussed;
		}

		return true;
	}

	void OnTextViewFocussed(object? sender, FocusEventArgs e)
	{
		if (element is not Editor mauiControl || control is not UITextView textView)
		{
			throw new InvalidOperationException("The Platform View or the Maui control isn't the right types.");
		}

		if (mauiControl.IsFocused)
		{
			textView.SelectAll(textView);
		}
	}
}