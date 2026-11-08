using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

// The window has no UIWindow: a window that UIKit presents the PopupPage in stays alive through UIKit and .NET MAUI after it is destroyed, whatever
// the popup registered, so the popup could not be collected there for a reason that has nothing to do with the popup.
[Category(TestCategory.Popup)]
[Collection(TestCollections.DisableParallelization)]
public class PopupWindowTests : CommunityToolkitHandlerTestBase
{
	// A scene that disconnects destroys its window without closing the popup that the window presents, and without navigating away from it.
	[Fact]
	public async Task PopupIsReleasedWhenItsWindowIsDestroyedWhileItIsOpen()
	{
		SetupBuilder();
		var popupReference = await InvokeOnMainThreadAsync(static () => OpenPopupAndDestroyItsWindow(openAgain: false, closePopupFirst: false));

		try
		{
			await VisualBehaviorHelpers.WaitForGCAsync(popupReference);
		}
		catch (XunitException exception)
		{
			throw new XunitException("The Popup is still alive after its window was destroyed while it was open and every reference to it was dropped: " +
				"the global UIKeyboard observers that Popup.OnPlatformPopupOpened registers capture the popup, and are removed only when the popup closes or is navigated away from, " +
				$"so they retain it. {exception.Message}");
		}
	}

	// Shows that the popup of the test above is retained because it was open: the same popup is collected once it was closed.
	[Fact]
	public async Task PopupIsReleasedWhenItIsClosedBeforeItsWindowIsDestroyed()
	{
		SetupBuilder();
		var popupReference = await InvokeOnMainThreadAsync(static () => OpenPopupAndDestroyItsWindow(openAgain: false, closePopupFirst: true));

		await VisualBehaviorHelpers.WaitForGCAsync(popupReference);
	}

	// A popup that is shown again registers its observers again: the registrations of the first time must not stay behind.
	[Fact]
	public async Task PopupIsReleasedWhenItIsOpenedAgainBeforeItsWindowIsDestroyed()
	{
		SetupBuilder();
		var popupReference = await InvokeOnMainThreadAsync(static () => OpenPopupAndDestroyItsWindow(openAgain: true, closePopupFirst: false));

		await VisualBehaviorHelpers.WaitForGCAsync(popupReference);
	}

	// Not inlined, so that no local of the test keeps the popup on the stack of the main thread while it is collected.
	[MethodImpl(MethodImplOptions.NoInlining)]
	static WeakReference OpenPopupAndDestroyItsWindow(bool openAgain, bool closePopupFirst)
	{
		var popup = new Popup { Content = new Label { Text = "Open while its window is destroyed" } };
		var window = new Window(new PopupPage(popup, new PopupOptions { Shape = null }));

		popup.NotifyPopupIsOpened();

		if (openAgain)
		{
			popup.NotifyPopupIsOpened();
		}

		if (closePopupFirst)
		{
			popup.NotifyPopupIsClosed();
		}

		((IWindow)window).Destroying();

		return new WeakReference(popup);
	}
}
