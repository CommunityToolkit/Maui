using CommunityToolkit.Core.Platform;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Core.Handlers;

public partial class PopupViewHandler : ElementHandler<IPopup, MCTPopup>
{
	protected override MCTPopup CreateNativeElement()
	{
		ArgumentNullException.ThrowIfNull(MauiContext);
		return new MCTPopup(MauiContext);
	}

	protected override void ConnectHandler(MCTPopup nativeView)
	{
		nativeView.SetElement(VirtualView);
		base.ConnectHandler(nativeView);
	}

	public static void MapOnDismissed(PopupViewHandler handler, IPopup view, object? result)
	{
		handler.DisconnectHandler(handler.NativeView);
	}

	public static void MapOnOpened(PopupViewHandler handler, IPopup view, object? result)
	{
		handler?.NativeView.Show();
	}

	public static void MapOnLightDismiss(PopupViewHandler handler, IPopup view, object? result)
	{
		view.LightDismiss();
		handler.DisconnectHandler(handler.NativeView);
	}

	public static void MapAnchor(PopupViewHandler handler, IPopup view)
	{
		handler?.NativeView.ConfigureControl();
	}

	public static void MapLightDismiss(PopupViewHandler handler, IPopup view)
	{
		handler?.NativeView.ConfigureControl();
	}

	public static void MapColor(PopupViewHandler handler, IPopup view)
	{
		handler?.NativeView.SetColor(view);
		handler?.NativeView.ConfigureControl();
	}

	protected override void DisconnectHandler(MCTPopup nativeView)
	{
		nativeView.CleanUp();
	}

	public static void MapSize(PopupViewHandler handler, IPopup view)
	{
		handler?.NativeView.ConfigureControl();
	}
}
