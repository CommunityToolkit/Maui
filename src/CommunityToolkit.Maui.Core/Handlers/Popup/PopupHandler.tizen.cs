using CommunityToolkit.Maui.Core.Views;
using Tizen.UIExtensions.NUI;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class PopupHandler : Microsoft.Maui.Handlers.ElementHandler<IPopup, MauiPopup>
{
	/// <summary>
	/// Action that's triggered when the Popup is closed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnClosed(PopupHandler handler, IPopup view, object? result)
	{
		var popup = handler.PlatformView;

		if (popup.IsOpen)
		{
			popup.Close();
		}

		handler.DisconnectHandler(popup);
	}

	/// <summary>
	/// Action that's triggered when the Popup is Opened.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">We don't need to provide the result parameter here.</param>
	public static void MapOnOpened(PopupHandler handler, IPopup view, object? result)
	{
		handler.PlatformView.ShowPopup();
	}

	/// <summary>
	/// Action that's triggered when the Popup is dismissed by tapping outside of the Popup.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view, object? result)
	{
		if (view.CanBeDismissedByTappingOutsideOfPopup)
		{
			view.OnDismissedByTappingOutsideOfPopup();
		}
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Anchor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapAnchor(PopupHandler handler, IPopup view)
	{
		// On Tizen, Anchor only update when popup is opened
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.CanBeDismissedByTappingOutsideOfPopup"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapCanBeDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view)
	{
		// this property directly access on platform view
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Color"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapColor(PopupHandler handler, IPopup view)
	{
		// this property directly access on platform view
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapSize(PopupHandler handler, IPopup view)
	{
		handler.PlatformView.UpdateContentSize();
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiPopup platformView)
	{
		platformView.SetElement(VirtualView);
	}

	/// <inheritdoc/>
	protected override MauiPopup CreatePlatformElement()
	{
		var mauiContext = MauiContext ?? throw new InvalidOperationException("${nameof(MauiContext)} cannot be null");
		return new MauiPopup(mauiContext);
	}
}