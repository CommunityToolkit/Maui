namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// Handler Popup control
/// </summary>
public partial class PopupHandler
{
	/// <summary>
	/// PropertyMapper for Popup Control
	/// </summary>
	public static PropertyMapper<IPopup, PopupHandler> PopUpMapper = new(ElementMapper)
	{
		[nameof(IPopup.Anchor)] = MapAnchor,
		[nameof(IPopup.Color)] = MapColor,
		[nameof(IPopup.Size)] = MapSize,
		[nameof(IPopup.VerticalOptions)] = MapSize,
		[nameof(IPopup.HorizontalOptions)] = MapSize,
		[nameof(IPopup.CanBeDismissedByTappingOutsideOfPopup)] = MapCanBeDismissedByTappingOutsideOfPopup
	};

	/// <summary>
	/// <see cref ="CommandMapper"/> for Popup Control.
	/// </summary>
	public static CommandMapper<IPopup, PopupHandler> PopUpCommandMapper = new(ElementCommandMapper)
	{
		[nameof(IPopup.OnClosed)] = MapOnClosed,
#if !(IOS || MACCATALYST)
		[nameof(IPopup.OnOpened)] = MapOnOpened,
#endif
		[nameof(IPopup.OnDismissedByTappingOutsideOfPopup)] = MapOnDismissedByTappingOutsideOfPopup
	};

	/// <summary>
	/// Constructor for <see cref="PopupHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="PropertyMapper"/>, if it's null the <see cref="PopUpMapper"/> will be used</param>
	/// <param name="commandMapper">Custom instance of <see cref="CommandMapper"/>, if it's null the <see cref="PopUpCommandMapper"/> will be used</param>
	public PopupHandler(PropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? PopUpMapper, commandMapper ?? PopUpCommandMapper)
	{
	}

	/// <summary>
	/// Default Constructor for <see cref="PopupHandler"/>.
	/// </summary>
	public PopupHandler()
		: base(PopUpMapper, PopUpCommandMapper)
	{
	}
}

#if !(IOS || ANDROID || MACCATALYST || WINDOWS)
public partial class PopupHandler : Microsoft.Maui.Handlers.ElementHandler<IPopup, object>
{
	/// <inheritdoc/>
	protected override object CreatePlatformElement() => throw new NotSupportedException();

	/// <summary>
	/// Action that's triggered when the Popup is closed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnClosed(PopupHandler handler, IPopup view, object? result)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup is Opened.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">We don't need to provide the result parameter here.</param>
	public static void MapOnOpened(PopupHandler handler, IPopup view, object? result)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup is dismissed by tapping outside of the Popup.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view, object? result)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Anchor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapAnchor(PopupHandler handler, IPopup view)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.CanBeDismissedByTappingOutsideOfPopup"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapCanBeDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Color"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapColor(PopupHandler handler, IPopup view)
	{
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapSize(PopupHandler handler, IPopup view)
	{
	}
}
#endif