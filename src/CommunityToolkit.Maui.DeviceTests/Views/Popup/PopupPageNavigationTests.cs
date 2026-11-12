using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// The headless runners have no window to push a PopupPage on, so these tests raise the navigation callbacks that .NET MAUI raises on the
// PopupPage of a Popup that another Popup is shown above, and then closed again.
[Category(TestCategory.Popup)]
[Collection(TestCollections.DisableParallelization)]
public partial class PopupPageNavigationTests : CommunityToolkitHandlerTestBase
{
	[Fact]
	public void OpenedIsRaisedOnlyOnceWhenAPopupAboveItIsClosed()
	{
		SetupBuilder();
		var lowerPopup = new Popup();
		var openedCount = 0;
		lowerPopup.Opened += (_, _) => openedCount++;
		var lowerPage = new NavigablePopupPage(lowerPopup);
		var upperPage = new NavigablePopupPage(new Popup());

		lowerPage.RaiseNavigatedTo();
		upperPage.ShowAbove(lowerPage);
		upperPage.CloseAbove(lowerPage);

		Assert.Equal(1, openedCount);
	}

	sealed class NavigablePopupPage(Popup popup) : PopupPage(popup, new PopupOptions { Shape = null })
	{
		public void RaiseNavigatedTo() => OnNavigatedTo(CreateEventArgs<NavigatedToEventArgs>());

		public void RaiseNavigatedFrom() => OnNavigatedFrom(CreateEventArgs<NavigatedFromEventArgs>());

		// .NET MAUI raises NavigatedFrom on the page below, then NavigatedTo on the page that is shown
		public void ShowAbove(NavigablePopupPage lowerPage)
		{
			lowerPage.RaiseNavigatedFrom();
			RaiseNavigatedTo();
		}

		// .NET MAUI raises NavigatedFrom on the page that closes, then NavigatedTo on the page below it, which is shown again
		public void CloseAbove(NavigablePopupPage lowerPage)
		{
			RaiseNavigatedFrom();
			lowerPage.RaiseNavigatedTo();
		}

		// The constructors of the event arguments are internal to .NET MAUI, and PopupPage does not read the arguments
		static T CreateEventArgs<T>() where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
	}
}