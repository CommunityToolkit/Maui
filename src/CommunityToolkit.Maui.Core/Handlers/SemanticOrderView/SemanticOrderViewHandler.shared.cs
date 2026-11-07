using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;
#if IOS || MACCATALYST
using PlatformContentView = Microsoft.Maui.Platform.ContentView;
#elif ANDROID || TIZEN
using PlatformContentView = Microsoft.Maui.Platform.ContentViewGroup;
#elif WINDOWS
using PlatformContentView = Microsoft.Maui.Platform.ContentPanel;
#else
using PlatformContentView = System.Object;
#endif

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// Handler SemanticOrderView control
/// </summary>
public partial class SemanticOrderViewHandler : ContentViewHandler
{
	/// <summary>
	/// PropertyMapper for SemanticOrderView Control
	/// </summary>
	public static IPropertyMapper<ISemanticOrderView, SemanticOrderViewHandler> SemanticOrderViewMapper = new PropertyMapper<ISemanticOrderView, SemanticOrderViewHandler>(ContentViewHandler.Mapper)
	{
		[nameof(ISemanticOrderView.ViewOrder)] = MapViewOrder,
	};
	/// <summary>
	/// <see cref ="CommandMapper"/> for SemanticOrderView Control.
	/// </summary>
	public static CommandMapper<ISemanticOrderView, SemanticOrderViewHandler> SemanticOrderViewCommandMapper = new(CommandMapper)
	{
	};

	/// <summary>
	/// Constructor for <see cref="SemanticOrderViewHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="PropertyMapper"/>, if it's null the <see cref="SemanticOrderViewMapper"/> will be used</param>
	/// <param name="commandMapper">Custom instance of <see cref="CommandMapper"/>, if it's null the <see cref="SemanticOrderViewCommandMapper"/> will be used</param>
	public SemanticOrderViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? SemanticOrderViewMapper, commandMapper ?? SemanticOrderViewCommandMapper)
	{
	}

	/// <summary>
	/// Default Constructor for <see cref="SemanticOrderViewHandler"/>.
	/// </summary>
	public SemanticOrderViewHandler() : base(SemanticOrderViewMapper, SemanticOrderViewCommandMapper)
	{

	}

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	public override void SetVirtualView(IView view)
	{
		ArgumentNullException.ThrowIfNull(view);

		var platformViewFactoryOfTheApp = PlatformViewFactory;
		if (platformViewFactoryOfTheApp is null || ((IElementHandler)this).PlatformView is not null)
		{
			base.SetVirtualView(view);
		}
		else
		{
			SetVirtualViewWithItsOwnPlatformView(view, platformViewFactoryOfTheApp);
		}

#if WINDOWS || IOS || MACCATALYST || ANDROID || TIZEN
		if (PlatformView is MauiSemanticOrderView semanticOrderView)
		{
			semanticOrderView.VirtualView = (ISemanticOrderView)VirtualView;
		}
#endif
	}

	static void MapViewOrder(SemanticOrderViewHandler handler, ISemanticOrderView view)
	{
#if WINDOWS || IOS || MACCATALYST || ANDROID || TIZEN
		if (handler.PlatformView is MauiSemanticOrderView semanticOrderView)
		{
			semanticOrderView.UpdateViewOrder();
		}
#endif
	}

	// MAUI creates the platform view of every ContentViewHandler, this one included, with the ContentViewHandler.PlatformViewFactory of the app, and
	// only a MauiSemanticOrderView applies ViewOrder. MAUI asks for the platform view before anything else in base.SetVirtualView, before the content
	// gets its handlers, so the factory of the app is back in place for them, for every other ContentViewHandler, and for any code that reads it.
	void SetVirtualViewWithItsOwnPlatformView(IView view, Func<ViewHandler<IContentView, PlatformContentView>, PlatformContentView> platformViewFactoryOfTheApp)
	{
		Func<ViewHandler<IContentView, PlatformContentView>, PlatformContentView>? platformViewFactoryOfTheHandler = null;
		platformViewFactoryOfTheHandler = handler =>
		{
			// Handlers are created on the UI thread, so only a handler that another thread creates in the meantime gets here, and it gets the
			// platform view that it got before
			if (!ReferenceEquals(handler, this))
			{
				return platformViewFactoryOfTheApp(handler);
			}

			PutBackThePlatformViewFactoryOfTheApp();
			return CreatePlatformView();
		};

		PlatformViewFactory = platformViewFactoryOfTheHandler;
		try
		{
			base.SetVirtualView(view);
		}
		finally
		{
			// MAUI does not ask for a platform view when base.SetVirtualView returns early or throws first
			PutBackThePlatformViewFactoryOfTheApp();
		}

		// A factory that was set in the meantime replaced this one, and is kept
		void PutBackThePlatformViewFactoryOfTheApp()
		{
			if (ReferenceEquals(PlatformViewFactory, platformViewFactoryOfTheHandler))
			{
				PlatformViewFactory = platformViewFactoryOfTheApp;
			}
		}
	}
}