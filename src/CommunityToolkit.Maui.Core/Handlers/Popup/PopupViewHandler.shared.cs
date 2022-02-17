using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Core.Handlers;

/// <summary>
/// Handler Popup control
/// </summary>
public partial class PopupViewHandler
{
	/// <summary>
	/// PropertyMapper for Popup Control
	/// </summary>
	public static PropertyMapper<IPopup, PopupViewHandler> PopUpMapper = new(ElementMapper)
	{
		[nameof(IPopup.Anchor)] = MapAnchor,
		[nameof(IPopup.Color)] = MapColor,
		[nameof(IPopup.Size)] = MapSize,
		[nameof(IPopup.VerticalOptions)] = MapSize,
		[nameof(IPopup.HorizontalOptions)] = MapSize,
		[nameof(IPopup.IsLightDismissEnabled)] = MapLightDismiss
	};

	/// <summary>
	/// <see cref ="CommandMapper"/> for Popup Control.
	/// </summary>
	public static CommandMapper<IPopup, PopupViewHandler> PopUpCommandMapper = new(ElementCommandMapper)
	{
		[nameof(IPopup.OnDismissed)] = MapOnDismissed,
		[nameof(IPopup.OnOpened)] = MapOnOpened,
		[nameof(IPopup.LightDismiss)] = MapOnLightDismiss
	};

	/// <summary>
	/// Constructor for <see cref="PopupViewHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="PropertyMapper"/>, if it's null the <see cref="PopUpMapper"/> will be used</param>
	/// <param name="commandMapper">Custom instance of <see cref="CommandMapper"/>, if it's null the <see cref="PopUpCommandMapper"/> will be used</param>
	public PopupViewHandler(PropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? PopUpMapper, commandMapper ?? PopUpCommandMapper)
	{
	}

	/// <summary>
	/// Default Constructor for <see cref="PopupViewHandler"/>.
	/// </summary>
	public PopupViewHandler()
		: base(PopUpMapper, PopUpCommandMapper)
	{
	}
}

#if !(ANDROID || IOS || MACCATALYST || WINDOWS)
public partial class PopupViewHandler : Microsoft.Maui.Handlers.ElementHandler<IPopup, object>
{
	/// <inheritdoc/>
	protected override object CreateNativeElement() => throw new NotImplementedException();

	/// <summary>
	/// Action that's triggered when the Popup is Dismissed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnDismissed(PopupViewHandler handler, IPopup view, object? result)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup is Opened.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">We don't need to provide the result parameter here.</param>
	public static void MapOnOpened(PopupViewHandler handler, IPopup view, object? result)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup is LightDismissed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnLightDismiss(PopupViewHandler handler, IPopup view, object? result)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Anchor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapAnchor(PopupViewHandler handler, IPopup view)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.IsLightDismissEnabled"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapLightDismiss(PopupViewHandler handler, IPopup view)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Color"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapColor(PopupViewHandler handler, IPopup view)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapSize(PopupViewHandler handler, IPopup view)
	{
	}
}
#endif

