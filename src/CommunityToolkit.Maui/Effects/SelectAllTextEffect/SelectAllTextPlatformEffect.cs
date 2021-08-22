using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Platform;

#if __ANDROID__
using Android.Widget;
#elif __IOS__
using ObjCRuntime;
using UIKit;
#endif

namespace CommunityToolkit.Maui.Effects
{
    public class SelectAllTextPlatformEffect : PlatformEffect
	{
#if __ANDROID__
		EditText EditText => (EditText)Control;
#endif

		public SelectAllTextPlatformEffect()
			: base()
		{
		}

		protected override void OnAttached()
		{
#if __ANDROID__
			EditText?.SetSelectAllOnFocus(true);
#elif __IOS__
			ApplyEffect(true);
#endif
		}

		protected override void OnDetached()
		{
#if __ANDROID__
			EditText?.SetSelectAllOnFocus(false);
#elif __IOS__
			ApplyEffect(false);
#endif
		}

#if __IOS__
		void ApplyEffect(bool apply) => ApplyToControl(Control, apply);

		bool ApplyToControl<T>(T controlType, bool apply) => controlType switch
		{
			UITextField textField => ApplyToUITextField(textField, apply),
			UITextView => ApplyToUITextView(apply),
			_ => throw new NotSupportedException($"Control of type: {controlType?.GetType()?.Name} is not supported by this effect.")
		};

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

			textfield.PerformSelector(new Selector("selectAll"), null, 0.0f);
		}

		bool ApplyToUITextView(bool apply)
		{
			if (Element is not Editor formsControl)
				return false;

			if (apply)
				formsControl.Focused += OnTextViewFocussed;
			else
				formsControl.Focused -= OnTextViewFocussed;

			return true;
		}

		void OnTextViewFocussed(object? sender, FocusEventArgs e)
		{
			if (Element is not Editor formsControl)
				return;

			if (Control is not UITextView textView)
				return;

			if (formsControl.IsFocused)
			{
				textView.SelectAll(textView);
			}
		}
#endif
	}
}