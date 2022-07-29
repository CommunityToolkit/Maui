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
		
	}

	public static global::Android.Views.View? GetViewForAccessibility(this IElement visualElement)
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

	public static global::Android.Views.View? GetViewForAccessibility(this IElement element, global::Android.Views.View platformView)
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
