using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class PopupHandler : ElementHandler<IPopup, MauiPopup>
{
	internal AView? Container { get; set; }
	internal int LastPopupWidth { get; set; }
	internal int LastPopupHeight { get; set; }
	internal double LastWindowWidth { get; set; }
	internal double LastWindowHeight { get; set; }

	/// <summary>
	/// Action that's triggered when the Popup is closed
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnClosed(PopupHandler handler, IPopup view, object? result)
	{
		var popup = handler.PlatformView;

		if (popup.IsShowing)
		{
			popup.Dismiss();
		}

		view.HandlerCompleteTCS.TrySetResult();

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
		handler.PlatformView.Show();
	}

	/// <summary>
	/// Action that's triggered when the Popup is dismissed by tapping outside of the popup.
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
		handler.PlatformView.SetAnchor(view);
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
		handler.PlatformView.SetColor(view);
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapSize(PopupHandler handler, IPopup view)
	{
		ArgumentNullException.ThrowIfNull(handler.Container);

		handler.PlatformView.SetSize(view, handler.Container, handler);
	}

	/// <inheritdoc/>
	protected override MauiPopup CreatePlatformElement()
	{
		_ = MauiContext ?? throw new InvalidOperationException("MauiContext is null, please check your MauiApplication.");
		_ = MauiContext.Context ?? throw new InvalidOperationException("Android Context is null, please check your MauiApplication.");

		return new MauiPopup(MauiContext.Context, MauiContext);
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiPopup platformView)
	{
		Container = platformView.SetElement(VirtualView);

		if (Container is not null)
		{
			Container.LayoutChange += OnLayoutChange;
		}
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiPopup platformView)
	{
		platformView.Dispose();

		if (Container is not null)
		{
			Container.LayoutChange -= OnLayoutChange;
		}
	}

	void OnShowed(object? sender, EventArgs args)
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null");

		VirtualView.OnOpened();
	}

	void OnLayoutChange(object? sender, EventArgs e)
	{
		if (VirtualView?.Handler?.PlatformView is Dialog dialog && Container is not null)
		{
			PopupExtensions.SetSize(dialog, VirtualView, Container, this);
		}
	}
}