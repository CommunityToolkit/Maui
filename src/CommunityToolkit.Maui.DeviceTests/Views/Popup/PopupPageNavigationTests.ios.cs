using CommunityToolkit.Maui.Views;
using CoreGraphics;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupPageNavigationTests
{
	const double keyboardHeight = 300;

	[Fact]
	public async Task PopupRespondsToTheKeyboardAgainWhenAPopupAboveItIsClosed()
	{
		SetupBuilder();
		var lowerPage = new NavigablePopupPage(CreatePopup());
		var upperPage = new NavigablePopupPage(CreatePopup());

		await AttachAndRun(lowerPage, async handler =>
		{
			await AssertionExtensions.AssertEventually(() => lowerPage.Content.PopupBorder.Width > 0, timeout: 5000, message: "The PopupPage was never laid out.");
			var viewController = handler.ViewController ?? throw new InvalidOperationException("The PopupPage has no view controller.");

			lowerPage.RaiseNavigatedTo();

			try
			{
				// Shows that this test observes the reaction to the keyboard, so that the assertions below can only fail because the popup lost it.
				await ShowKeyboard(viewController);
				Assert.True(viewController.AdditionalSafeAreaInsets.Bottom > 0, "The popup should keep the keyboard out of its safe area while it is open.");
				await HideKeyboard();
				Assert.Equal(0, viewController.AdditionalSafeAreaInsets.Bottom);

				upperPage.ShowAbove(lowerPage);
				upperPage.CloseAbove(lowerPage);

				await ShowKeyboard(viewController);
				Assert.True(viewController.AdditionalSafeAreaInsets.Bottom > 0,
					"The popup should keep the keyboard out of its safe area again once the popup that was shown above it is closed.");
				await HideKeyboard();
			}
			finally
			{
				lowerPage.RaiseNavigatedFrom();
			}

			// The popup must not keep a second set of observers from the time it was set up again
			await ShowKeyboard(viewController);
			Assert.Equal(0, viewController.AdditionalSafeAreaInsets.Bottom);
		});
	}

	static Popup CreatePopup() => new()
	{
		Padding = 0,
		Content = new Label { Text = nameof(PopupPageNavigationTests) }
	};

	static Task ShowKeyboard(UIViewController viewController) => InvokeOnMainThreadAsync(() =>
	{
		var windowBounds = (viewController.View?.Window ?? throw new InvalidOperationException("The PopupPage is not in a window.")).Bounds;
		var keyboardFrame = new CGRect(windowBounds.X, windowBounds.Bottom - keyboardHeight, windowBounds.Width, keyboardHeight);
		var userInfo = NSDictionary.FromObjectsAndKeys(
			[NSValue.FromCGRect(keyboardFrame), NSValue.FromCGRect(keyboardFrame), NSNumber.FromDouble(0), NSNumber.FromInt32(7)],
			[UIKeyboard.FrameBeginUserInfoKey, UIKeyboard.FrameEndUserInfoKey, UIKeyboard.AnimationDurationUserInfoKey, UIKeyboard.AnimationCurveUserInfoKey]);

		NSNotificationCenter.DefaultCenter.PostNotificationName(UIKeyboard.WillChangeFrameNotification, null, userInfo);
	});

	static Task HideKeyboard() => InvokeOnMainThreadAsync(() => NSNotificationCenter.DefaultCenter.PostNotificationName(UIKeyboard.WillHideNotification, null));
}