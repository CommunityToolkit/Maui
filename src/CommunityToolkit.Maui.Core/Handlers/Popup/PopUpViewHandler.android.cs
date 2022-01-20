using System;
using CommunityToolkit.Core.Platform;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;
using AView = Android.Views.View;

namespace CommunityToolkit.Core.Handlers;

public partial class PopupViewHandler : ElementHandler<IBasePopup, MCTPopup>
{
	internal AView? Container { get; set; }

	public static void MapOnDismissed(PopupViewHandler handler, IBasePopup view, object? result)
	{
		var popup = handler.NativeView;

		if (popup.IsShowing)
		{
			popup.Dismiss();
		}

		handler.DisconnectHandler(popup);
	}

	public static void MapOnOpened(PopupViewHandler handler, IBasePopup view, object? result)
	{
		handler.NativeView?.Show();
	}

	public static void MapOnLightDismiss(PopupViewHandler handler, IBasePopup view, object? result)
	{
		if (view.IsLightDismissEnabled)
		{
			view.LightDismiss();
		}
	}

	public static void MapAnchor(PopupViewHandler handler, IBasePopup view)
	{
		handler.NativeView?.SetAnchor(view);
	}

	public static void MapLightDismiss(PopupViewHandler handler, IBasePopup view)
	{
		handler.NativeView?.SetLightDismiss(view);
	}

	public static void MapColor(PopupViewHandler handler, IBasePopup view)
	{
		handler.NativeView?.SetColor(view);
	}

	public static void MapSize(PopupViewHandler handler, IBasePopup view)
	{
		handler.NativeView?.SetSize(view, handler.Container);
		handler.NativeView?.SetAnchor(view);
	}

	protected override MCTPopup CreateNativeElement()
	{
		_ = MauiContext ?? throw new NullReferenceException("MauiContext is null, please check your MauiApplication.");
		_ = MauiContext.Context ?? throw new NullReferenceException("Android Context is null, please check your MauiApplication.");

		return new MCTPopup(MauiContext.Context, MauiContext);
	}

	protected override void ConnectHandler(MCTPopup nativeView)
	{
		Container = nativeView.SetElement(VirtualView);
	}

	void OnShowed(object? sender, EventArgs args)
	{
		VirtualView?.OnOpened();
	}

	protected override void DisconnectHandler(MCTPopup nativeView)
	{
		nativeView.Dispose();
	}
}
