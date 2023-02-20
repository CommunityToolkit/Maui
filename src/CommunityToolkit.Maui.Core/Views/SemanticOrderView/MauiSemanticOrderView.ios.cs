using System.Collections.Generic;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the <see href="SemanticOrderView"/> control.
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
		this.SetAccessibilityElements(NSArray.FromNSObjects(GetAccessibilityElements().ToArray()));
	}

	IEnumerable<NSObject> GetAccessibilityElements()
	{
		if (VirtualView is null)
		{
			yield break;
		}

		var viewOrder = VirtualView.ViewOrder;

		foreach (var view in viewOrder)
		{
			if (view.Handler is IPlatformViewHandler pvh &&
				pvh.PlatformView is not null)
			{
				yield return pvh.PlatformView;
			}
		}
	}
}
