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

