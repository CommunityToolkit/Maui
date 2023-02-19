using System.Collections.Generic;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the SemanticOrderView control.
/// </summary>
public class MauiSemanticOrderView : ContentView, IUIAccessibilityContainer
{
	ISemanticOrderView? virtualView;
	internal ISemanticOrderView? VirtualView
	{
		get => virtualView;
		set
		{
			virtualView = value;
			UpdateViewOrder();
		}
	}

	internal void UpdateViewOrder()
	{
		var result = GetAccessibilityElements();

		if (result != null)
		{
			this.SetAccessibilityElements(NSArray.FromNSObjects(result.ToArray()));
		}
	}

	List<NSObject>? GetAccessibilityElements()
	{
		if (VirtualView is null)
		{
			return null;
		}

		var viewOrder = VirtualView.ViewOrder;

		var returnValue = new List<NSObject>();
		foreach (var view in viewOrder)
		{
			if (view.Handler is IPlatformViewHandler pvh &&
				pvh.PlatformView is not null)
			{
				returnValue.Add(pvh.PlatformView);
			}
		}

		return returnValue.Count == 0 ? null : returnValue;
	}
}
