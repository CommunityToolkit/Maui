using System.Diagnostics.CodeAnalysis;
using AVKit;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.Handlers;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on iOS and macOS.
/// </summary>
public class MauiMediaElement : UIView
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="playerViewController">The <see cref="AVPlayerViewController"/> that acts as the platform media player.</param>
	/// <param name="virtualView">The <see cref="MediaElement"/> used as the VirtualView for this <see cref="MauiMediaElement"/>.</param>
	/// <exception cref="NullReferenceException">Thrown when <paramref name="playerViewController"/><c>.View</c> is <see langword="null"/>.</exception>
	public MauiMediaElement(AVPlayerViewController playerViewController, MediaElement virtualView)
	{
		ArgumentNullException.ThrowIfNull(playerViewController.View);
		playerViewController.View.Frame = Bounds;
#if IOS16_0_OR_GREATER || MACCATALYST16_1_OR_GREATER
		// On iOS 16+ and macOS 13+ the AVPlayerViewController has to be added to a parent ViewController, otherwise the transport controls won't be displayed.

		UIViewController? viewController = null;

		// If any of the Parents in the VisualTree of MediaElement uses a UIViewController for their PlatformView, use it as the child ViewController
		// This enables support for UI controls like CommunityToolkit.Maui.Popup whose PlatformView is a UIViewController (e.g. `public class MauiPopup : UIViewController`)
		// To find the UIViewController, we traverse `MediaElement.Parent` until a Parent using UIViewController is located
		if (virtualView.TryFindParentPlatformView(out UIViewController? parentUIViewController))
		{
			viewController = parentUIViewController;
		}
		// If none of the Parents in the VisualTree of MediaElement use a UIViewController, we can use the ViewController in the PageHandler
		// To find the PageHandler, we traverse `MediaElement.Parent` until the Page is located
		else if (virtualView.TryFindParent<Page>(out var page)
			&& page.Handler is PageHandler { ViewController: not null } pageHandler)
		{
			viewController = pageHandler.ViewController;
		}
		// If the parent Page cannot be found, MediaElement is being used inside a DataTemplate. I.e. The MediaElement is being used inside a CarouselView or a CollectionView
		// The top-most parent is null when MediaElement is placed in a DataTemplate because DataTemplates defer loading until they are about to be displayed on the screen 
		// When the MediaElement is used inside a DataTemplate, we must retrieve its CarouselViewHandler / CollectionViewHandler
		// To retrieve its CarouselViewHandler / CollectionViewHandler, we must traverse all VisualElements on the current page
		else
		{
			ArgumentNullException.ThrowIfNull(virtualView);

			if (!TryGetCurrentPage(out var currentPage))
			{
				throw new InvalidOperationException("Cannot find current page");
			}

			// look for an ItemsView (e.g. CarouselView or CollectionView) on page 
			TryGetItemsViewOnPage(currentPage, out var itemsView);

			// Set the viewController to the first root view controller.
			viewController = Platform.GetCurrentUIViewController();
			
			// Check to see if there is a ItemsView in a collection view or CarouselView and replace Shell Renderer with the correct handler
			viewController = itemsView?.Where(item => item.Handler is not null)
					 .Select(item => GetUIViewController(item.Handler))
					 .FirstOrDefault(viewController => viewController is not null); 
		}

		if (viewController?.View is not null)
		{
			// Zero out the safe area insets of the AVPlayerViewController
			UIEdgeInsets insets = viewController.View.SafeAreaInsets;
			playerViewController.AdditionalSafeAreaInsets =
				new UIEdgeInsets(insets.Top * -1, insets.Left, insets.Bottom * -1, insets.Right);

			// Add the View from the AVPlayerViewController to the parent ViewController
			viewController.AddChildViewController(playerViewController);
		}
#endif
		AddSubview(playerViewController.View);
	}

	static UIViewController? GetUIViewController(IViewHandler? handler)
	{
		return handler switch
		{
			CarouselViewHandler carouselViewHandler => carouselViewHandler.ViewController,
			CarouselViewHandler2 carouselViewHandler2 => carouselViewHandler2.ViewController,
			CollectionViewHandler collectionViewHandler => collectionViewHandler.ViewController,
			CollectionViewHandler2 collectionViewHandler2 => collectionViewHandler2.ViewController,
			null => throw new InvalidOperationException("Handler cannot be null"),
			_ => throw new NotSupportedException($"{handler.GetType()} not yet supported")
		};
	}
	static void TryGetItemsViewOnPage(List<Page> currentPage, out List<ItemsView> itemsView)
	{
		// We are looking for an ItemsView (e.g. CarouselView or CollectionView) on page.
		// To retrieve its CarouselViewHandler / CollectionViewHandler, we must traverse all VisualElements on the current page/
		// We check if Handler.PlatformView is a View to ensure we are looking at a VisualElement. If not, we continue to the next VisualElement.
		// We need to check both the page and ItemsView to ensure we are looking at the correct VisualElement.
		// Checking both the page and ItemsView is necessary because the ItemsView may be nested inside another VisualElement.
		// We may be using Multi-window support, so we need to traverse all Windows to find the current page in a multi-window application.
		// We then check if the VisualElement is an ItemsView (e.g. CarouselView or CollectionView) and add it to the itemsView list/

		itemsView = [];
		List<ItemsView> itemsViewsOnPage = [];
		currentPage.Where(page => page.Handler?.PlatformView is View)
			 .SelectMany(page => ((IElementController)page).Descendants().OfType<ItemsView>())
			 .Where(item => item.Handler?.PlatformView is View)
			 .ToList()
			 .ForEach(item => itemsViewsOnPage.Add(item));
	}

	static bool TryGetCurrentPage([NotNullWhen(true)] out List<Page> currentPage)
	{
		currentPage = [];

		if (Application.Current?.Windows is null)
		{
			return false;
		}

		if (Application.Current.Windows.Count is 0)
		{
			throw new InvalidOperationException("Unable to find active Window");
		}

		if (Application.Current.Windows.Count > 1)
		{
			// We traverse all Windows to find the current page in a multi-window application
			// We check if the Window contains a Page and add it to the currentPage list
			// If the Page is null we continue to the next Window
			// We then return the currentPage list
			var pages = new List<Page>();
			var list = Application.Current.Windows.ToList();
			pages.AddRange(from item in list
						   where item.Page is not null
						   select item.Page);
			currentPage = pages;
			return true;
		}

		var window = Application.Current.Windows[0];

		// If using Shell, return the current page
		if (window.Page is Shell { CurrentPage: not null } shell)
		{
			currentPage.Add(shell.CurrentPage);
			return true;
		}

		// If not using Shell, use the ModelNavigationStack to check for any pages displayed modally
		if (TryGetModalPage(window, out var modalPage))
		{
			currentPage.Add(modalPage);
			return true;
		}

		// If not using Shell or a Modal Page, return the visible page in the (non-modal) NavigationStack
		if (window.Navigation.NavigationStack.LastOrDefault() is Page page)
		{
			currentPage.Add(page);
			return true;
		}

		return false;

		static bool TryGetModalPage(Window window, [NotNullWhen(true)] out Page? page)
		{
			page = window.Navigation.ModalStack.LastOrDefault();
			return page is not null;
		}
	}	
}