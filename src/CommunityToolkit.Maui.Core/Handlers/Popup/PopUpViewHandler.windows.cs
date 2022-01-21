using CommunityToolkit.Core.Platform;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Core.Handlers;

public partial class PopupViewHandler : ElementHandler<IBasePopup, PopupRenderer>
{
	protected override PopupRenderer CreateNativeElement()
	{
		ArgumentNullException.ThrowIfNull(MauiContext);
		return new PopupRenderer(MauiContext);
	}

	protected override void ConnectHandler(PopupRenderer nativeView)
	{
		nativeView.SetElement(VirtualView);
		base.ConnectHandler(nativeView);
	}

	public static void MapOnDismissed(PopupViewHandler handler, IBasePopup view, object? result)
	{
		handler.DisconnectHandler(handler.NativeView);
	}

	public static void MapOnOpened(PopupViewHandler handler, IBasePopup view, object? result)
	{
		handler?.NativeView.Show();
	}

	public static void MapOnLightDismiss(PopupViewHandler handler, IBasePopup view, object? result)
	{
		view.LightDismiss();
		handler.DisconnectHandler(handler.NativeView);
	}

	public static void MapAnchor(PopupViewHandler handler, IBasePopup view)
	{
		handler?.NativeView.SetLayout(view);
	}

	public static void MapLightDismiss(PopupViewHandler handler, IBasePopup view)
	{
	}

	public static void MapColor(PopupViewHandler handler, IBasePopup view)
	{
		handler?.NativeView.SetColor(view);
	}

	protected override void DisconnectHandler(PopupRenderer nativeView)
	{
		nativeView.CleanUp();
	}

	public static void MapSize(PopupViewHandler handler, IBasePopup view)
	{
		//handler?.NativeView.SetSize(view);
		handler?.NativeView.SetLayout(view);
	}
}
