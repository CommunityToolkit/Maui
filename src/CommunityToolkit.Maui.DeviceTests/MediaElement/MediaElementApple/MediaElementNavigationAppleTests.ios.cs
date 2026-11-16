using AVKit;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// A MediaElement in pages that a real .NET MAUI Window shows, when navigation takes its page or its part of the page out of the window and brings it back.
// iOS only: MauiWindowHostTestBase hosts a Window on iOS.
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaElementNavigationAppleTests : MauiWindowHostTestBase
{
	// #3066: when the user comes back to a ShellContent, Shell shows its page in a new view controller. iOS 26.1 and later threw
	// UIViewControllerHierarchyInconsistency there, because the player view controller was still a child of the page's previous view controller.
	[Fact]
	public Task ComingBackToAShellContentMovesThePlayerViewControllerToTheNewViewControllerOfThePage()
	{
		SetupBuilder(static builder => builder.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: false));
		var mediaElement = new MediaElement { WidthRequest = 200, HeightRequest = 100 };
		var firstPage = new ContentPage { Content = mediaElement };
		var shell = new Shell
		{
			Items =
			{
				new ShellContent { Route = "first", Content = firstPage },
				new ShellContent { Route = "second", Content = new ContentPage() },
			}
		};

		return RunInMauiWindowAsync(shell, isPagePresentedModally: false, firstPage, async _ =>
		{
			var view = Assert.IsType<MauiMediaElement>(mediaElement.Handler?.PlatformView);
			var controller = PlayerViewController(view);
			Assert.Same(NearestViewController(view), controller.ParentViewController);

			await shell.GoToAsync("//second").WaitAsync(TimeSpan.FromSeconds(5));
			await shell.GoToAsync("//first").WaitAsync(TimeSpan.FromSeconds(5));

			await AssertionExtensions.AssertEventually(() => view.Window is not null, timeout: 5000, message: "The page of the MediaElement did not come back into the window.");
			Assert.NotNull(controller.ParentViewController);
			Assert.Same(NearestViewController(view), controller.ParentViewController);
		});
	}

	// #3247: a TabView-like page, whose tab headers are a CollectionView, shows a MediaElement in the template of another CollectionView and puts that
	// tab's content back after another tab. MediaElement 10.0.0 threw NotSupportedException for a page with several ItemsViews.
	[Fact]
	public Task SwitchingBackToATabWithAMediaElementInACollectionViewKeepsThePlayerViewControllerAttached()
	{
		SetupBuilder(static builder => builder.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: false));
		var mediaElements = new List<MediaElement>();
		var firstTab = new VerticalStackLayout
		{
			new CollectionView
			{
				ItemsSource = new[] { "item1" },
				ItemTemplate = new DataTemplate(() =>
				{
					var mediaElement = new MediaElement { WidthRequest = 250, HeightRequest = 200 };
					mediaElements.Add(mediaElement);
					return mediaElement;
				})
			}
		};
		var tabContent = new ContentView { Content = firstTab };
		var tabHeaders = new CollectionView { ItemsSource = new[] { "Tab 1", "Tab 2" }, HeightRequest = 56 };
		var grid = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
		grid.Add(tabHeaders, 0, 0);
		grid.Add(tabContent, 0, 1);
		var page = new ContentPage { Content = grid };

		return RunInMauiWindowAsync(page, isPagePresentedModally: false, page, async _ =>
		{
			await AssertionExtensions.AssertEventually(() => ShownMediaElementView(mediaElements) is not null, timeout: 5000, message: "The MediaElement of the first tab was never shown.");
			var view = ShownMediaElementView(mediaElements)!;
			Assert.Same(NearestViewController(view), PlayerViewController(view).ParentViewController);

			tabContent.Content = new Label { Text = "Tab 2" };
			await AssertionExtensions.AssertEventually(() => view.Window is null, timeout: 5000, message: "The first tab did not leave the window.");

			tabContent.Content = firstTab;
			await AssertionExtensions.AssertEventually(() => ShownMediaElementView(mediaElements) is not null, timeout: 5000, message: "The MediaElement of the first tab did not come back.");
			var shownView = ShownMediaElementView(mediaElements)!;
			var controller = PlayerViewController(shownView);
			Assert.NotNull(controller.ParentViewController);
			Assert.Same(NearestViewController(shownView), controller.ParentViewController);
		});
	}

	static MauiMediaElement? ShownMediaElementView(IEnumerable<MediaElement> mediaElements) =>
		mediaElements.Select(static mediaElement => mediaElement.Handler?.PlatformView).OfType<MauiMediaElement>().FirstOrDefault(static view => view.Window is not null);

	// The view of the player view controller is the only subview of MauiMediaElement, and the next responder of a view controller's view is the view controller.
	static AVPlayerViewController PlayerViewController(MauiMediaElement view) =>
		Assert.IsType<AVPlayerViewController>(Assert.Single(view.Subviews).NextResponder, exactMatch: false);

	// The view controller that UIKit expects as the parent of the player view controller: the first one up the responder chain of MauiMediaElement.
	static UIViewController? NearestViewController(UIView view)
	{
		for (var responder = view.NextResponder; responder is not null; responder = responder.NextResponder)
		{
			if (responder is UIViewController viewController)
			{
				return viewController;
			}
		}

		return null;
	}
}