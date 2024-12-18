using System.Diagnostics;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
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
			var mauiContext = virtualView.Handler?.MauiContext ?? throw new InvalidOperationException($"Unable to retrieve {nameof(IMauiContext)}");
			var popupContent = (View)(virtualView.Content ?? throw new InvalidOperationException($"{nameof(IPopup.Content)} cannot be null."));

			if (virtualView is BindableObject bindableObject)
			{
				popupContent.SetBinding(BindingContextProperty, BindingBase.Create<BindableObject, object>(static bindable => bindable.BindingContext, source: bindableObject));
			}
			else
			{
				Trace.TraceInformation($"Unable to set {nameof(BindableObject.BindingContext)} for {nameof(IPopup)}.{nameof(IPopup.Content)} because {nameof(IPopup)} implementation does not inherit from {nameof(BindableObject)}");
			}

			var contentPage = new ContentPage
			{
				Content = popupContent
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

		if (view.Parent is not Element parent || handler.VirtualView is not Popup popup)
		{
			return;
		}

		if (popup.Content?.Parent is ContentPage contentPage)
		{
			parent.RemoveLogicalChild(contentPage);
		}

		parent.RemoveLogicalChild(popup);
	}
}