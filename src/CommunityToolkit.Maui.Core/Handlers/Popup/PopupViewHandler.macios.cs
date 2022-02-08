using CommunityToolkit.Core.Platform;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Core.Handlers;
public partial class PopupViewHandler : ElementHandler<IBasePopup, MCTPopup>
{
	public static async void MapOnDismissed(PopupViewHandler handler, IBasePopup view, object? result)
	{
		if (handler.NativeView is null)
		{
			return;
		}

		var vc = handler.NativeView.ViewController;
		if (vc is not null)
		{
			await vc.DismissViewControllerAsync(true);
		}

		handler.DisconnectHandler(handler.NativeView);
	}

	public static void MapOnOpened(PopupViewHandler handler, IBasePopup view, object? result)
	{
		view.OnOpened();
	}

	public static void MapOnLightDismiss(PopupViewHandler handler, IBasePopup view, object? result)
	{
		if (handler.NativeView is not MCTPopup popupRenderer)
		{
			return;
		}

		if (popupRenderer.IsViewLoaded && view.IsLightDismissEnabled)
		{
			view.LightDismiss();
		}
	}

	public static void MapAnchor(PopupViewHandler handler, IBasePopup view)
	{
		handler.NativeView?.SetSize(view);
		handler.NativeView?.SetLayout(view);
	}

	public static void MapLightDismiss(PopupViewHandler handler, IBasePopup view)
	{
		handler.NativeView?.SetLightDismiss(view);
	}

	public static void MapColor(PopupViewHandler handler, IBasePopup view)
	{
		handler.NativeView?.SetBackgroundColor(view);
	}

	public static void MapSize(PopupViewHandler handler, IBasePopup view)
	{
		handler.NativeView?.SetSize(view);
		handler.NativeView?.SetLayout(view);
	}

	protected override void ConnectHandler(MCTPopup nativeView)
	{
		base.ConnectHandler(nativeView);
		nativeView.SetElement(VirtualView);
	}

	protected override MCTPopup CreateNativeElement()
	{
		return new MCTPopup(MauiContext ?? throw new NullReferenceException(nameof(MauiContext)));
	}

	protected override void DisconnectHandler(MCTPopup nativeView)
	{
		base.DisconnectHandler(nativeView);
		nativeView?.CleanUp();
	}
}