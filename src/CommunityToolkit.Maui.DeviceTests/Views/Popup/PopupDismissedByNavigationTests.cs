using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// The headless runners have no window to push a PopupPage on, so these tests raise Window.ModalPopped, which .NET MAUI raises when navigation removes
// a modal page, on the Window that hosts the PopupPage.
[Category(TestCategory.Popup)]
[Collection(TestCollections.DisableParallelization)]
public class PopupDismissedByNavigationTests : CommunityToolkitHandlerTestBase
{
	[Fact]
	public void PopupClosedIsRaisedOnceWhenNavigationRemovesThePopupPageWithoutCloseAsync()
	{
		SetupBuilder();
		var popup = new Popup();
		var popupClosedCount = 0;
		var popupPageClosedCount = 0;
		popup.Closed += (_, _) => popupClosedCount++;
		var popupPage = new PopupPage(popup, new PopupOptions { Shape = null });
		popupPage.PopupClosed += (_, _) => popupPageClosedCount++;
		var window = new Window(popupPage);

		RaiseNavigatedTo(popupPage);
		RaiseModalPopped(window, popupPage);
		RaiseModalPopped(window, popupPage);

		Assert.Equal(1, popupClosedCount);
		Assert.Equal(1, popupPageClosedCount);
	}

	[Fact]
	public void PopupClosedIsNotRaisedWhenNavigationRemovesAnotherModalPage()
	{
		SetupBuilder();
		var popupPageClosedCount = 0;
		var popupPage = new PopupPage(new Popup(), new PopupOptions { Shape = null });
		popupPage.PopupClosed += (_, _) => popupPageClosedCount++;
		var window = new Window(popupPage);

		RaiseNavigatedTo(popupPage);
		RaiseModalPopped(window, new ContentPage());

		Assert.Equal(0, popupPageClosedCount);
	}

	// Window.OnModalPopped is internal to .NET MAUI
	static void RaiseModalPopped(Window window, Page modalPage)
	{
		var onModalPopped = typeof(Window).GetMethod("OnModalPopped", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("Window.OnModalPopped was not found");
		onModalPopped.Invoke(window, [modalPage]);
	}

	// PopupPage.OnNavigatedTo is protected, and the constructors of the event arguments are internal to .NET MAUI, which PopupPage does not read
	static void RaiseNavigatedTo(PopupPage popupPage)
	{
		var onNavigatedTo = typeof(Page).GetMethod("OnNavigatedTo", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("Page.OnNavigatedTo was not found");
		onNavigatedTo.Invoke(popupPage, [RuntimeHelpers.GetUninitializedObject(typeof(NavigatedToEventArgs))]);
	}
}
