using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the <see href="SemanticOrderView"/> control.
/// </summary>
public class MauiSemanticOrderView : ContentView, IUIAccessibilityContainer
{
	internal ISemanticOrderView? VirtualView
	{
		get;
		set
		{
			field = value;
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
			if (view.Handler is IPlatformViewHandler { PlatformView: not null } platformViewHandler)
			{
				yield return platformViewHandler.PlatformView;
			}
		}
	}
}