using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;
using UIKit;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class SemanticOrderViewHandler : ElementHandler<ISemanticOrderView, MauiSemanticOrderView>
{
	/// <summary>
	/// TODO
	/// </summary>
	/// <param name="handler"></param>
	/// <param name="view"></param>
	public static void MapViewOrder(SemanticOrderViewHandler handler, ISemanticOrderView view)
	{
		handler.PlatformView.SetAccessibilityElements(NSArray.FromNSObjects(GetAccessibilityElements(handler)?.ToArray()));
	}

	/// <inheritdoc/>
	protected override MauiSemanticOrderView CreatePlatformElement() => new();

	static List<NSObject>? GetAccessibilityElements(SemanticOrderViewHandler handler)
	{
		if (handler.VirtualView == null)
		{
			return null;
		}

		var viewOrder = handler.VirtualView.ViewOrder;

		var returnValue = new List<NSObject>();
		foreach (ISemanticOrderView view in viewOrder)
		{
			returnValue.Add(NSObject.FromObject(view.Handler?.PlatformView));
		}

		return returnValue.Count == 0 ? new() : returnValue;
	}
}
