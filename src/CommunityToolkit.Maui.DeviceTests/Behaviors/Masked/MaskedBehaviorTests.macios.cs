using UIKit;
using PlatformView = UIKit.UIView;

namespace CommunityToolkit.Maui.DeviceTests;

// iOS and Mac Catalyst: an Entry is a UITextField. UIKit delivers the keystrokes that are still pending inside the setter of
// UITextField.Text and then stores the value it was given, so a mask written after the Text change, from the dispatcher, dropped the
// characters typed in between ("abcdef" typed fast became "ab-cf"). The behavior applies the mask while the Text change is handled: these
// tests change the text through UIKit and assert the masked text before the UI thread runs anything else.
public partial class MaskedBehaviorTests
{
	static void SetPlatformText(PlatformView platformView, string text) => ((UITextField)platformView).Text = text;

	static string? GetPlatformText(PlatformView platformView) => ((UITextField)platformView).Text;

	// UITextInput.InsertText inserts at the caret and raises EditingChanged, as a keystroke of the keyboard does.
	[Fact]
	public async Task TextTypedIntoTheTextFieldIsMaskedBeforeTheNextKeystroke()
	{
		SetupBuilder();
		var entry = CreateEntryWithMask();
		(string Keystroke, string MaskedText)[] keystrokes = [("a", "a"), ("b", "ab"), ("c", "ab-c"), ("d", "ab-cd"), ("e", "ab-cd-e"), ("f", "ab-cd-ef")];

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var textField = Assert.IsType<UITextField>(handler.PlatformView, exactMatch: false);
			Assert.True(textField.BecomeFirstResponder(), "The text field did not become the first responder.");

			try
			{
				foreach (var (keystroke, maskedText) in keystrokes)
				{
					textField.InsertText(keystroke);

					Assert.Equal(maskedText, textField.Text);
					Assert.Equal(maskedText, entry.Text);
				}
			}
			finally
			{
				textField.ResignFirstResponder();
			}
		});
	}

	// Code that sets the text of the UITextField (MauiTextField raises TextPropertySet), with a text longer than the mask: every pass of the
	// behavior, until the text fits the mask, is done when the setter returns.
	[Fact]
	public async Task TextSetOnTheTextFieldIsMaskedBeforeTheSetterReturns()
	{
		SetupBuilder();
		var entry = CreateEntryWithMask();

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var textField = Assert.IsType<UITextField>(handler.PlatformView, exactMatch: false);

			textField.Text = "abcdefghij";

			Assert.Equal("ab-cd-ef", textField.Text);
			Assert.Equal("ab-cd-ef", entry.Text);
		});
	}
}