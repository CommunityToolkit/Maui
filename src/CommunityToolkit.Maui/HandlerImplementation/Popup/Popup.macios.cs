using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.Maui.Handlers;

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
			var contentPage = new ContentPage { Content = view };

			contentPage.Parent = Application.Current?.MainPage;
			contentPage.SetBinding(VisualElement.BindingContextProperty, new Binding { Source = virtualView, Path = VisualElement.BindingContextProperty.PropertyName });

			return (PageHandler)contentPage.ToHandler(mauiContext);
		}
	}
}