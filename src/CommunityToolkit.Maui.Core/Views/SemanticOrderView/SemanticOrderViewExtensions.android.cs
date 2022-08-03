using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Views;
using Microsoft.Maui.ApplicationModel;
using static Microsoft.Maui.Resource;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// TBD
/// </summary>
public static class SemanticOrderViewExtensions
{
	/// <summary>
	/// TBD
	/// </summary>
	/// <param name="element"></param>
	/// <param name="view"></param>
	public static void SetViewOrder(this View element, in ISemanticOrderView view)
	{
	//	SetAccessibilityElements(view);
	}

	static void SetAccessibilityElements(ISemanticOrderView virtualView)
	{
		if (virtualView == null)
		{
			return;
		}

		var viewOrder = virtualView.ViewOrder.OfType<IView>().ToList();

		for (var i = 1; i < viewOrder.Count; i++)
		{
			var view1 = viewOrder[i - 1].GetViewForAccessibility();
			var view2 = viewOrder[i].GetViewForAccessibility();

			if (view1 == null || view2 == null)
			{
				return;
			}

			view2.AccessibilityTraversalAfter = view1.Id;
			view1.AccessibilityTraversalBefore = view2.Id;
		}
	}

	public static global::Android.Views.View? GetViewForAccessibility(this IView visualElement)
	{
		var platformView = visualElement.Handler?.PlatformView;

		if (visualElement is Layout)
		{
			return platformView as View;
		}
		else if (platformView is ViewGroup vg && vg.ChildCount > 0)
		{
			return vg?.GetChildAt(0);
		}
		else if (platformView != null)
		{
			return (View)platformView;
		}

		return null;
	}

	public static global::Android.Views.View? GetViewForAccessibility(this IView element, global::Android.Views.View platformView)
	{
		if (platformView == null)
		{
			return element?.GetViewForAccessibility();
		}

		if (element is Layout)
		{
			return platformView;
		}
		else if (platformView is ViewGroup vg && vg.ChildCount > 0)
		{
			return vg?.GetChildAt(0);
		}

		return platformView;
	}
}
