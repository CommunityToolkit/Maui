using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.ViewManagement;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class PopupHandler : ElementHandler<IPopup, Popup>
{
	/// <summary>
	/// Action that's triggered when the Popup is Dismissed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnClosed(PopupHandler handler, IPopup view, object? result)
	{
		var window = view.GetWindow();
		if (window.Overlays.FirstOrDefault() is IWindowOverlay popupOverlay)
		{
			window.RemoveOverlay(popupOverlay);
		}

		view.HandlerCompleteTCS.TrySetResult();
		handler.DisconnectHandler(handler.PlatformView);
	}

	/// <summary>
	/// Action that's triggered when the Popup is Opened.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">We don't need to provide the result parameter here.</param>
	public static void MapOnOpened(PopupHandler handler, IPopup view, object? result)
	{
		ArgumentNullException.ThrowIfNull(view.Parent);
		ArgumentNullException.ThrowIfNull(handler.MauiContext);

		var parent = view.Parent.ToPlatform(handler.MauiContext);

		parent.IsHitTestVisible = false;
		handler.PlatformView.XamlRoot = parent.XamlRoot;
		handler.PlatformView.IsHitTestVisible = true;
		handler.PlatformView.IsOpen = true;

		AddOverlayToWindow(view.GetWindow());

		view.OnOpened();
	}


	/// <summary>
	/// Action that's triggered when the Popup is dismissed by tapping outside of the Popup.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view, object? result)
	{
		view.OnDismissedByTappingOutsideOfPopup();
		handler.DisconnectHandler(handler.PlatformView);
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Anchor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapAnchor(PopupHandler handler, IPopup view)
	{
		handler.PlatformView.SetAnchor(view, handler.MauiContext);
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.CanBeDismissedByTappingOutsideOfPopup"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapCanBeDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view)
	{
		handler.PlatformView.IsLightDismissEnabled = view.CanBeDismissedByTappingOutsideOfPopup;
		handler.PlatformView.LightDismissOverlayMode = LightDismissOverlayMode.Off;
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
		handler.PlatformView.SetSize(view, handler.MauiContext);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(Popup platformView)
	{
		if (VirtualView.Parent is null)
		{
			return;
		}

		ArgumentNullException.ThrowIfNull(VirtualView.Handler?.MauiContext);
		var parent = VirtualView.Parent.ToPlatform(VirtualView.Handler.MauiContext);
		parent.IsHitTestVisible = true;
		platformView.IsOpen = false;
		platformView.Closed -= OnClosed;
		if (MauiContext is not null)
		{
			MauiContext.GetPlatformWindow().SizeChanged -= OnSizeChanged;
		}
	}

	/// <inheritdoc/>
	protected override Popup CreatePlatformElement()
	{
		var popup = new Popup();
		return popup;
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(Popup platformView)
	{
		platformView.Closed += OnClosed;
		platformView.ConfigureControl(VirtualView, MauiContext);
		if (MauiContext is not null)
		{
			MauiContext.GetPlatformWindow().SizeChanged += OnSizeChanged;
		}
		base.ConnectHandler(platformView);
	}

	static void AddOverlayToWindow(IWindow window)
	{
		var uiSetting = new UISettings();
		var backgroundColor = uiSetting.GetColorValue(UIColorType.Background).ToColor();
		window.AddOverlay(new PopupOverlay(window, backgroundColor.IsDark()
													? Color.FromRgba(0, 0, 0, 153)
													: Color.FromRgba(255, 255, 255, 153))); // 60% Opacity
	}

	void OnClosed(object? sender, object e)
	{
		if (!PlatformView.IsOpen && VirtualView.CanBeDismissedByTappingOutsideOfPopup)
		{
			VirtualView.Handler?.Invoke(nameof(IPopup.OnDismissedByTappingOutsideOfPopup));
		}
	}

	void OnSizeChanged(object? sender, WindowSizeChangedEventArgs e)
	{
		if (VirtualView is not null)
		{
			PopupExtensions.SetSize(PlatformView, VirtualView, MauiContext);
			PopupExtensions.SetLayout(PlatformView, VirtualView, MauiContext);
		}
	}
}