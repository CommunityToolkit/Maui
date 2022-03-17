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
	public static async void MapOnClosed(PopupHandler handler, IPopup view, object? result)
	{
		var vc = handler.PlatformView.ViewController;
		if (vc is not null)
		{
			await vc.DismissViewControllerAsync(true);
		}

		handler.DisconnectHandler(handler.PlatformView);
	}

	/// <summary>
	/// Action that's triggered when the Popup is dismissed by tapping outside of the Popup.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view, object? result)
	{
		if (handler.PlatformView is not MauiPopup popupRenderer)
		{
			throw new InvalidOperationException($"{nameof(handler.PlatformView)} must be of type {typeof(PopupHandler)}");
		}

		if (popupRenderer.IsViewLoaded && view.CanBeDismissedByTappingOutsideOfPopup)
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
		handler.PlatformView.SetSize(view);
		handler.PlatformView.SetLayout(view);
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.CanBeDismissedByTappingOutsideOfPopup"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapCanBeDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view)
	{
		handler.PlatformView.SetCanBeDismissedByTappingOutsideOfPopup(view);
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Color"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapColor(PopupHandler handler, IPopup view)
	{
		handler.PlatformView.SetBackgroundColor(view);
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapSize(PopupHandler handler, IPopup view)
	{
		handler.PlatformView.SetSize(view);
		handler.PlatformView.SetLayout(view);
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiPopup nativeView)
	{
		base.ConnectHandler(nativeView);
		nativeView.SetElement(VirtualView);
	}

	/// <inheritdoc/>
	protected override MauiPopup CreatePlatformElement()
	{
		return new MauiPopup(MauiContext ?? throw new NullReferenceException(nameof(MauiContext)));
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiPopup nativeView)
	{
		base.DisconnectHandler(nativeView);
		PlatformView.CleanUp();
	}
}