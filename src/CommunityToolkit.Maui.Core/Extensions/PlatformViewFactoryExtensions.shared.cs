using Microsoft.Maui.Handlers;
#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace CommunityToolkit.Maui.Core.Extensions;

static class PlatformViewFactoryExtensions
{
	// MAUI creates the platform view of every handler of one of its handler types, such as ContentViewHandler or LayoutHandler, with the
	// PlatformViewFactory that an app can set for that type, also for a toolkit handler that derives from it and needs its own platform view.
	// MAUI asks for the platform view before anything else in SetVirtualView, before the views inside get their handlers, so the factory of the
	// app is back in place for them, for every other handler of that type, and for any code that reads it.
	public static void SetVirtualViewWithItsOwnPlatformView<TVirtualView, TPlatformView>(this ViewHandler<TVirtualView, TPlatformView> handler, IView view,
		Action<IView> setVirtualViewOfTheBaseHandler, Func<TPlatformView> createPlatformViewOfTheHandler)
		where TVirtualView : class, IView
#if IOS || MACCATALYST || ANDROID || WINDOWS
		where TPlatformView : PlatformView
#else
		where TPlatformView : class
#endif
	{
		var platformViewFactoryOfTheApp = ViewHandler<TVirtualView, TPlatformView>.PlatformViewFactory;
		if (platformViewFactoryOfTheApp is null || ((IElementHandler)handler).PlatformView is not null)
		{
			setVirtualViewOfTheBaseHandler(view);
			return;
		}

		Func<ViewHandler<TVirtualView, TPlatformView>, TPlatformView>? platformViewFactoryOfTheHandler = null;
		platformViewFactoryOfTheHandler = handlerOfThePlatformView =>
		{
			// Handlers are created on the UI thread, so only a handler that another thread creates in the meantime gets here, and it gets the
			// platform view that it got before
			if (!ReferenceEquals(handlerOfThePlatformView, handler))
			{
				return platformViewFactoryOfTheApp(handlerOfThePlatformView);
			}

			PutBackThePlatformViewFactoryOfTheApp();
			return createPlatformViewOfTheHandler();
		};

		ViewHandler<TVirtualView, TPlatformView>.PlatformViewFactory = platformViewFactoryOfTheHandler;
		try
		{
			setVirtualViewOfTheBaseHandler(view);
		}
		finally
		{
			// MAUI does not ask for a platform view when SetVirtualView returns early or throws first
			PutBackThePlatformViewFactoryOfTheApp();
		}

		// A factory that was set in the meantime replaced this one, and is kept
		void PutBackThePlatformViewFactoryOfTheApp()
		{
			if (ReferenceEquals(ViewHandler<TVirtualView, TPlatformView>.PlatformViewFactory, platformViewFactoryOfTheHandler))
			{
				ViewHandler<TVirtualView, TPlatformView>.PlatformViewFactory = platformViewFactoryOfTheApp;
			}
		}
	}
}