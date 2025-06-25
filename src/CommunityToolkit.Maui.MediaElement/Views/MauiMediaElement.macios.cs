using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

		UIViewController? viewController;

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
			if (TryGetItemsViewOnPage(currentPage, out var itemsView))
			{
				var parentViewController = itemsView.Handler switch
				{
					CarouselViewHandler carouselViewHandler => carouselViewHandler.ViewController ?? GetInternalControllerForItemsView(carouselViewHandler),
					CarouselViewHandler2 carouselViewHandler2 => carouselViewHandler2.ViewController ?? GetInternalControllerForItemsView2(carouselViewHandler2),
					CollectionViewHandler collectionViewHandler => collectionViewHandler.ViewController ?? GetInternalControllerForItemsView(collectionViewHandler),
					CollectionViewHandler2 collectionViewHandler2 => collectionViewHandler2.ViewController ?? GetInternalControllerForItemsView2(collectionViewHandler2),
					null => throw new InvalidOperationException("Handler cannot be null"),
					_ => throw new NotSupportedException($"{itemsView.Handler.GetType()} not yet supported")
				};

				viewController = parentViewController;

				// The Controller we need is a `protected internal` property called ItemsViewController in the ItemsViewHandler class: https://github.com/dotnet/maui/blob/cf002538cb73db4bf187a51e4786d7478a7025ee/src/Controls/src/Core/Handlers/Items/ItemsViewHandler.iOS.cs#L39
				// In this method, we must use reflection to get the value of its backing field 
				static ItemsViewController<TItemsView> GetInternalControllerForItemsView<TItemsView>(ItemsViewHandler<TItemsView> handler) where TItemsView : ItemsView
				{
					var nonPublicInstanceFields = typeof(ItemsViewHandler<TItemsView>).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

					var controllerProperty = nonPublicInstanceFields.Single(x => x.FieldType == typeof(ItemsViewController<TItemsView>));
					return (ItemsViewController<TItemsView>)(controllerProperty.GetValue(handler) ?? throw new InvalidOperationException($"Unable to get the value for the Controller property on {handler.GetType()}"));
				}

				// The Controller we need is a `protected internal` property called ItemsViewController in the ItemsViewHandler2 class: https://github.com/dotnet/maui/blob/70e8ddfd4bd494bc71aa7afb812cc09161cf0c72/src/Controls/src/Core/Handlers/Items2/ItemsViewHandler2.iOS.cs#L64
				// In this method, we must use reflection to get the value of its backing field 
				static ItemsViewController<TItemsView> GetInternalControllerForItemsView2<TItemsView>(ItemsViewHandler2<TItemsView> handler) where TItemsView : ItemsView
				{
					var nonPublicInstanceFields = typeof(ItemsViewHandler2<TItemsView>).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

					var controllerProperty = nonPublicInstanceFields.Single(x => x.FieldType == typeof(ItemsViewController2<TItemsView>));
					return (ItemsViewController<TItemsView>)(controllerProperty.GetValue(handler) ?? throw new InvalidOperationException($"Unable to get the value for the Controller property on {handler.GetType()}"));
				}
			}
			// If we don't find an ItemsView, default to the current UIViewController
			else
			{
				viewController = Platform.GetCurrentUIViewController();
			}
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

	static bool TryGetItemsViewOnPage(Page currentPage, [NotNullWhen(true)] out ItemsView? itemsView)
	{
		var itemsViewsOnPage = ((IElementController)currentPage).Descendants().OfType<ItemsView>().ToList();
		switch (itemsViewsOnPage.Count)
		{
			case > 1:
				// We are unable to determine which ItemsView contains the MediaElement when multiple ItemsView are being used in the same page
				// TODO: Add support for MediaElement in an ItemsView on a Page containing multiple ItemsViews 
				throw new NotSupportedException("MediaElement does not currently support pages containing multiple ItemsViews (eg multiple CarouselViews + CollectionViews)");
			case 1:
				itemsView = itemsViewsOnPage[0];
				return true;
			case <= 0:
				itemsView = null;
				return false;
		}
	}

	static bool TryGetCurrentPage([NotNullWhen(true)] out Page? currentPage)
	{
		currentPage = null;

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
			// We are unable to determine which Window contains the ItemsView that contains the MediaElement when multiple ItemsView are being used in the same page
			// TODO: Add support for MediaElement in an ItemsView in a multi-window application
			throw new NotSupportedException("MediaElement is not currently supported in multi-window applications");
		}
		if (Application.Current.Windows[0].Page is Page page)
		{
			currentPage = PageExtensions.GetCurrentPage(page);
			return true;
		}
		return false;
	}
}