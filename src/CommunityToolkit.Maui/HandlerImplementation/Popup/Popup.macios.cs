using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;
namespace CommunityToolkit.Maui.Views;

public partial class Popup : Element
{
	void AddHandlerChanged()
	{
		HandlerChanged += OnHandlerChanged;
	}

	void RemoveHandlerChanged()
	{
		HandlerChanged -= OnHandlerChanged;
		if (Handler?.MauiContext is IMauiContext mauiContext)
		{
			var platformPopup = this.ToHandler(mauiContext);
			if (platformPopup.PlatformView is MauiPopup dialog &&
				platformPopup.VirtualView is IPopup pPopup &&
				Content is Element content)
			{
				RemovePropertyChanged(content, dialog, pPopup);
			}
		}
	}

	void OnHandlerChanged(object? sender, EventArgs e)
	{
		if (Handler?.MauiContext is IMauiContext mauiContext)
		{
			var platformPopup = this.ToHandler(mauiContext);
			if (platformPopup.PlatformView is MauiPopup dialog &&
				platformPopup.VirtualView is IPopup pPopup &&
				Content is Element content)
			{
				AddPropertyChanged(content, dialog, pPopup);
			}
		}
	}

	void RemovePropertyChanged(Element target, MauiPopup dialog, IPopup pPopup)
	{
		if (target is View view)
		{
			view.PropertyChanged -= OnPropertyChanged;
		}

		foreach (Element element in target.LogicalChildrenInternal)
		{
			RemovePropertyChanged(element, dialog, pPopup);
		}
	}

	void AddPropertyChanged(Element target, MauiPopup dialog, IPopup pPopup)
	{
		if (target is View view)
		{
			view.PropertyChanged += OnPropertyChanged;
		}

		foreach (Element element in target.LogicalChildrenInternal)
		{
			AddPropertyChanged(element, dialog, pPopup);
		}
	}

	void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (Handler?.MauiContext is IMauiContext mauiContext)
		{
			var platformPopup = this.ToHandler(mauiContext);
			if (platformPopup.PlatformView is MauiPopup dialog &&
				platformPopup.VirtualView is IPopup pPopup)
			{
				if (e.PropertyName == "Text")
				{
					CommunityToolkit.Maui.Core.Views.PopupExtensions.SetSize(dialog, pPopup);
					CommunityToolkit.Maui.Core.Views.PopupExtensions.SetLayout(dialog, pPopup);
				}
			}
		}
	}

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
				Content = view,
				Parent = virtualView.Parent as Element
			};

			return (PageHandler)contentPage.ToHandler(mauiContext);
		}
	}
}