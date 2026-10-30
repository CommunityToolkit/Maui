using System.Diagnostics;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A behavior that selects all text when the view is focused.
/// </summary>
public class SelectAllTextBehavior : BasePlatformBehavior<InputView>
{
	// Static, so that the handler removed when the behavior detaches is the one added when it attached.
	// Internal for the device tests: .NET MAUI raises Focused only with IsFocused true, so only a direct call reaches the other branch.
	internal static void OnTextViewFocussed(object? sender, FocusEventArgs e)
	{
		if (e.IsFocused && sender is VisualElement { Handler.PlatformView: UITextView textView })
		{
			textView.SelectAll(textView);
		}
	}

	/// <inheritdoc/>
	protected override void OnAttachedTo(InputView bindable, UIView platformView)
	{
		base.OnAttachedTo(bindable, platformView);

		ApplyBehaviorToControl(true, bindable, platformView);
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(InputView bindable, UIView platformView)
	{
		base.OnDetachedFrom(bindable, platformView);

		ApplyBehaviorToControl(false, bindable, platformView);
	}

	static void ApplyBehaviorToControl<T>(bool apply, InputView inputView, T platformView) where T : UIView
	{
		switch (inputView, platformView)
		{
			case (_, UITextField textField):
				ApplyToTextField(textField, apply);
				break;

			case (Editor editor, UITextView):
				ApplyToTextView(editor, apply);
				break;

			default:
				throw new NotSupportedException($"Control of type: {platformView.GetType().Name} is not supported by this effect.");
		}
	}

	static void ApplyToTextField(UITextField textField, bool shouldApply)
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

		static void OnEditingDidBegin(object? sender, EventArgs e)
		{
			if (sender is not UITextField textfield)
			{
				throw new InvalidOperationException($"The Platform View should be of the type {nameof(UITextField)}.");
			}

			textfield.PerformSelector(new Selector("selectAll"), null, 0.0f);
		}
	}

	// MacCatalyst support blocked: https://github.com/xamarin/xamarin-macios/issues/15156
	static void ApplyToTextView(VisualElement mauiControl, bool shouldApply)
	{
		if (OperatingSystem.IsMacCatalyst())
		{
			Trace.WriteLine($"WARNING: {nameof(SelectAllTextBehavior)} does not support {typeof(Editor).FullName} on MacCatalyst. For more information, see https://github.com/CommunityToolkit/Maui/issues/432");
		}

		if (shouldApply)
		{
			mauiControl.Focused += OnTextViewFocussed;
		}
		else
		{
			mauiControl.Focused -= OnTextViewFocussed;
		}
	}
}