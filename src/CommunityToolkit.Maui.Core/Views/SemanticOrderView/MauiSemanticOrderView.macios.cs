using Microsoft.Maui.Platform;
using ObjCRuntime;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the <see href="SemanticOrderView"/> control.
/// </summary>
/// <remarks>
/// The views of <see cref="ISemanticOrderView.ViewOrder"/> are the first accessibility elements of the container, in that order. The rest of
/// its content stays accessible: it follows them, in the order of the view hierarchy.
/// </remarks>
public class MauiSemanticOrderView : ContentView, IUIAccessibilityContainer
{
	internal ISemanticOrderView? VirtualView { get; set; }

	// The accessibility elements are built from ViewOrder whenever UIKit asks for them (GetAccessibilityElementsInViewOrder): there is
	// nothing to apply to the native view when ViewOrder changes
	internal void UpdateViewOrder()
	{
	}

	// UIKit asks the container for its accessibility elements whenever it needs them: they are the platform views of ViewOrder at that
	// moment, followed by the rest of the container's content. So a view of the order that got its platform view since ViewOrder was set is
	// listed too, also when nothing laid the container out again (adding a view below a view with a fixed size does not).
	// With nothing to order, UIKit's default (nil) is returned: the container then exposes its subviews in their default order. A non-nil
	// array is the complete list of the container's accessibility elements, so an empty one would hide every child, and the content that is
	// not in the order must be listed too, or VoiceOver cannot reach it.
	[Export("accessibilityElements")]
	NSArray? GetAccessibilityElementsInViewOrder()
	{
		var orderedElements = GetPlatformViewsInViewOrder().ToList();
		if (orderedElements.Count is 0)
		{
			return null;
		}

		var orderedViews = orderedElements.Select(static element => element.Handle).ToHashSet();
		var viewsThatHoldOrderedViews = new HashSet<NativeHandle>();
		foreach (var orderedView in orderedElements.OfType<UIView>())
		{
			for (var superview = orderedView.Superview; superview is not null && superview.Handle != Handle; superview = superview.Superview)
			{
				viewsThatHoldOrderedViews.Add(superview.Handle);
			}
		}

		var accessibilityElements = new List<NSObject>(orderedElements);
		AddTheRestOfTheContent(this);

		return NSArray.FromNSObjects(accessibilityElements.ToArray());

		// The content of the view, in the order of the view hierarchy, without the views of the order. A subview that holds no view of the
		// order is listed as it is: UIKit exposes it, or what it holds, as it would without an order. So is an accessibility element, which
		// UIKit does not look into. UIKit would look into any other view that holds views of the order and expose them a second time: what
		// else that view holds is listed instead. Hidden views stay hidden, as they are without an order.
		void AddTheRestOfTheContent(UIView view)
		{
			foreach (var subview in view.Subviews)
			{
				if (subview.Hidden || subview.AccessibilityElementsHidden || orderedViews.Contains(subview.Handle))
				{
					continue;
				}

				if (viewsThatHoldOrderedViews.Contains(subview.Handle) && !subview.IsAccessibilityElement)
				{
					AddTheRestOfTheContent(subview);
				}
				else
				{
					accessibilityElements.Add(subview);
				}
			}
		}
	}

	IEnumerable<NSObject> GetPlatformViewsInViewOrder()
	{
		// Called by UIKit: an invalid (null) ViewOrder is nothing to order rather than an exception
		if (VirtualView?.ViewOrder is not IEnumerable<IView> viewOrder)
		{
			yield break;
		}

		foreach (var view in viewOrder)
		{
			if (view.Handler?.PlatformView is NSObject platformView)
			{
				yield return platformView;
			}
		}
	}
}