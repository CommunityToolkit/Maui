using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class PopupViewHandler : ElementHandler<IPopup, MauiPopup>
{
	internal AView? Container { get; set; }

	/// <summary>
	/// Action that's triggered when the Popup is Dismissed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnDismissed(PopupViewHandler handler, IPopup view, object? result)
	{
		var popup = handler.NativeView;

		if (popup.IsShowing)
		{
			popup.Dismiss();
		}

		handler.DisconnectHandler(popup);
	}

	/// <summary>
	/// Action that's triggered when the Popup is Opened.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">We don't need to provide the result parameter here.</param>
	public static void MapOnOpened(PopupViewHandler handler, IPopup view, object? result)
	{
		handler.NativeView?.Show();
	}

	/// <summary>
	/// Action that's triggered when the Popup is dismissed by tapping outside of the Popup.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnLightDismissed(PopupViewHandler handler, IPopup view, object? result)
	{
		if (view.IsLightDismissEnabled)
		{
			view.OnLightDismissed();
		}
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Anchor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapAnchor(PopupViewHandler handler, IPopup view)
	{
		handler.NativeView?.SetAnchor(view);
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.IsLightDismissEnabled"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapLightDismiss(PopupViewHandler handler, IPopup view)
	{
		handler.NativeView?.SetLightDismiss(view);
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Color"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapColor(PopupViewHandler handler, IPopup view)
	{
		handler.NativeView?.SetColor(view);
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapSize(PopupViewHandler handler, IPopup view)
	{
		ArgumentNullException.ThrowIfNull(handler.Container);

		handler.NativeView?.SetSize(view, handler.Container);
		handler.NativeView?.SetAnchor(view);
	}

	/// <inheritdoc/>
	protected override MauiPopup CreateNativeElement()
	{
		_ = MauiContext ?? throw new InvalidOperationException("MauiContext is null, please check your MauiApplication.");
		_ = MauiContext.Context ?? throw new InvalidOperationException("Android Context is null, please check your MauiApplication.");

		return new MauiPopup(MauiContext.Context, MauiContext);
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiPopup nativeView)
	{
		Container = nativeView.SetElement(VirtualView);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiPopup nativeView)
	{
		nativeView.Dispose();
	}

	void OnShowed(object? sender, EventArgs args)
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null");

		VirtualView.OnOpened();
	}
}
