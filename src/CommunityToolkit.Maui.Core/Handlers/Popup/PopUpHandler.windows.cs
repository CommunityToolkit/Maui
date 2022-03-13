using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class PopupHandler : ElementHandler<IPopup, MauiPopup>
{
	/// <summary>
	/// Action that's triggered when the Popup is Dismissed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnDismissed(PopupHandler handler, IPopup view, object? result)
	{
		handler.DisconnectHandler(handler.NativeView);
	}

	/// <summary>
	/// Action that's triggered when the Popup is Opened.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">We don't need to provide the result parameter here.</param>
	public static void MapOnOpened(PopupHandler handler, IPopup view, object? result)
	{
		handler.NativeView.Show();
	}

	/// <summary>
	/// Action that's triggered when the Popup is dismissed by tapping outside of the Popup.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnLightDismissed(PopupHandler handler, IPopup view, object? result)
	{
		view.OnLightDismissed();
		handler.DisconnectHandler(handler.NativeView);
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Anchor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapAnchor(PopupHandler handler, IPopup view)
	{
		handler?.NativeView.ConfigureControl();
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.IsLightDismissEnabled"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapLightDismiss(PopupHandler handler, IPopup view)
	{
		handler.NativeView.ConfigureControl();
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Color"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapColor(PopupHandler handler, IPopup view)
	{
		handler.NativeView.SetColor(view);
		handler.NativeView.ConfigureControl();
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapSize(PopupHandler handler, IPopup view)
	{
		handler.NativeView.ConfigureControl();
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiPopup nativeView)
	{
		nativeView.CleanUp();
	}

	/// <inheritdoc/>
	protected override MauiPopup CreateNativeElement()
	{
		ArgumentNullException.ThrowIfNull(MauiContext);
		return new MauiPopup(MauiContext);
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiPopup nativeView)
	{
		nativeView.SetElement(VirtualView);
		base.ConnectHandler(nativeView);
	}
}
