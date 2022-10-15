using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class SemanticOrderViewHandler : ViewHandler<ISemanticOrderView, MauiSemanticOrderView>
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

	static List<NSObject>? GetAccessibilityElements(SemanticOrderViewHandler handler)
	{
		if (handler.VirtualView == null)
		{
			return null;
		}

		var viewOrder = handler.VirtualView.ViewOrder;

		var returnValue = new List<NSObject>();
		foreach (IView view in viewOrder)
		{	
			returnValue.Add(view.ToPlatform(handler.MauiContext!));
		}

		return returnValue.Count == 0 ? null : returnValue;
	}

	/// <inheritdoc/>
	protected override MauiSemanticOrderView CreatePlatformView()
	{
		return new MauiSemanticOrderView
		{
			CrossPlatformArrange = VirtualView.CrossPlatformArrange,
			CrossPlatformMeasure = VirtualView.CrossPlatformMeasure,
			VirtualView = VirtualView
		};
	}
}
