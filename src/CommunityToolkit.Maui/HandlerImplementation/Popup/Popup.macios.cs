using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Views;

public partial class Popup
{
	/// <summary>
	/// Action that's triggered when the Popup is Opened.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">We don't need to provide the result parameter here.</param>
	public static void MapOnOpened(PopupHandler handler, IPopup view, object? result)
	{
		handler.PlatformView?.CreateControl(CreatePageHandler, view);
		view.OnOpened();


		static PageHandler CreatePageHandler(IPopup virtualView)
		{
			var mauiContext = virtualView.Handler?.MauiContext ?? throw new NullReferenceException(nameof(IMauiContext));
			var view = (View?)virtualView.Content ?? throw new InvalidOperationException($"{nameof(IPopup.Content)} can't be null here.");
			view.SetBinding(BindingContextProperty, new Binding { Source = virtualView, Path = BindingContextProperty.PropertyName });
			var contentPage = new ContentPage
			{
				Content = view
			};
			var parent = virtualView.Parent as Element;
			parent?.AddLogicalChild(contentPage);

			return (PageHandler)contentPage.ToHandler(mauiContext);
		}
	}

	/// <summary>
	/// Action that's triggered when the Popup is Closed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">We don't need to provide the result parameter here.</param>
	public static void MapOnClosed(PopupHandler handler, IPopup view, object? result)
	{
		PopupHandler.MapOnClosed(handler, view, result);

		var parent = view.Parent as Element;
		if (parent is not null)
		{
			if (handler.VirtualView is Popup popup)
			{
				if (popup.Content is not null)
				{
					if (popup.Content.Parent is ContentPage contentPage)
					{
						parent.RemoveLogicalChild(contentPage);
					}
				}
				parent.RemoveLogicalChild(popup);
			}
		}
	}
}